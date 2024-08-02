/*
-----------------------------------------------------------------------------------
TRACER FOUNDATION -
Toolset for Realtime Animation, Collaboration & Extended Reality

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://research.animationsinstitut.de/tracer 
https://github.com/FilmakademieRnd/TRACER

TRACER FOUNDATION is a development by Filmakademie Baden-Wuerttemberg,
Animationsinstitut R&D Labs in the scope of the EU funded project
MAX-R (101070072) and funding on the own behalf of Filmakademie Baden-Wuerttemberg.
Former EU projects Dreamspace (610005) and SAUCE (780470) have inspired the
TRACER FOUNDATION development.

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/
//! @file "SceneReceiverModule.cs"
//! @brief Implementation of the scene receiver module, sending scene requests and receives scene data. 
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 25.06.2021

using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using GLTF.Schema;

namespace tracer
{
    //!
    //! The scene receiver module, sending scene requests and receives scene data.
    //!
    public class SceneGLTFReceiverModule : NetworkManagerModule
    {
        //!
        //! The event that is triggerd, when the scene has been received.
        //!
        public event EventHandler m_sceneReceived;

        //!
        //! The menu for the network configuration.
        //!
        private MenuTree m_menu;

        //!
        //! Constructor
        //!
        //! @param  name  The  name of the module.
        //! @param core A reference to the TRACER core.
        //!
        public SceneGLTFReceiverModule(string name, Manager manager) : base(name, manager)
        {
            if (core.isServer)
                load = false;
        }

        private StreamingCycle m_streamingCycle;
        private StreamingState m_streamingState;

        //! 
        //!  Function called when an Unity Awake() callback is triggered
        //! 
        //! @param sender A reference to the TRACER core.
        //! @param e Arguments for these event. 
        //! 
        protected override void Init(object sender, EventArgs e)
        {
        }

        //! 
        //! Function called when an Unity Start() callback is triggered
        //! 
        //! @param sender A reference to the TRACER core.
        //! @param e Arguments for these event. 
        //! 
        protected override void Start(object sender, EventArgs e)
        {
            manager.connectUsingQrCode += ReceiveSceneUsingQr;
            Parameter<Action> button = new Parameter<Action>(Connect, "Connect");

            List<AbstractParameter> parameterList1 = new List<AbstractParameter>
            {
                new Parameter<string>(null, "Server"),
                new Parameter<string>(null, "Device")
            };

            m_menu = new MenuTree()
                .Begin(MenuItem.IType.VSPLIT)
                    .Begin(MenuItem.IType.HSPLIT)
                         .Add("Scene Source")
                         .Add(new ListParameter(parameterList1, "Device"))
                     .End()
                     .Begin(MenuItem.IType.HSPLIT)
                         .Add("IP Address")
                         .Add(manager.settings.ipAddress)
                     .End()
                     .Begin(MenuItem.IType.HSPLIT)
                         .Add(button)
                     .End()
                .End();

            m_menu.iconResourceLocation = "Images/button_network";
            m_menu.caption = "glTF Network Settings";
            UIManager uiManager = core.getManager<UIManager>();
            uiManager.addMenu(m_menu);

            // add elements to start menu;
            uiManager.startMenu
                .Begin(MenuItem.IType.HSPLIT)
                    .Add("HTTP server address")
                    .Add(manager.settings.ipAddress)
                .End()
                .Begin(MenuItem.IType.HSPLIT)
                     .Add(button)
                .End();

            //core.getManager<UIManager>().showMenu(m_menu);
        }

        private void Connect()
        {
            Helpers.Log(manager.settings.ipAddress.value);

            core.getManager<UIManager>().hideMenu();

            receiveScene(manager.settings.ipAddress.value, "8080");
        }

        //!
        //! Function that overrides the default start function.
        //! Because of Unity's single threded design we have to 
        //! split the work within a coroutine.
        //!
        //! @param ip IP address of the network interface.
        //! @param port Port number to be used.
        //!
        protected override void start(string ip, string port)
        {
            m_ip = ip;
            m_port = port;

            NetworkManager.threadCount++;


            core.StartCoroutine(startReceive());
        }

        private async Task InitializeState()
        {
            var uri = "localhost:8080/cubespotlight.glb";
            var downloader = new GLBDownloader(uri);
            var glbFile = await downloader.DownloadBasicGLBFile();
            var gltfRoot = await JsonUtils.DeserializeAsync(glbFile.Json);
            m_streamingState = StreamingState.Instance;
            m_streamingState.GLBFile = glbFile;
            m_streamingState.GLTFRoot = gltfRoot;
            m_streamingState.ResourceUri = uri;
            m_streamingState.RootTransform = GameObject.Find("/Scene").transform;
            m_streamingState.NetworkSettings = new NetworkSettings()
            {
                Patience = 1000
            };
            m_streamingState.AverageDownloadSpeed = GLBDownloader.SpeedTracker.CalculateAverageSpeed();
            m_streamingState.NoLevelOfDetail = false;
            m_streamingState.NodeBoundingBoxes = new Dictionary<ExtendedNode, NodeBoundingBox>();
            m_streamingState.BudgetStrategy = BudgetStrategy.Distance;
        }

        //!
        //! Coroutine that creates a new thread receiving the scene data
        //! and yielding to allow the main thread to update the statusDialog.
        //!
        private IEnumerator startReceive()
        {
            var initTask = InitializeState();
            yield return new WaitUntil(() => initTask.IsCompleted);

            var cycleTask = StartNewCycle(async () =>
            {
                //_renderTasks.Add(_cycle.TryRender());
                var shouldStartNewCycle = !m_streamingCycle.HasRenderedAllNodes();
                if (shouldStartNewCycle)
                {
                    await m_streamingCycle.Execute();
                    var root = GameObject.Find("/Scene");
                    AddSceneObjectComponentsRecursively(root);
                }
            }, () =>
            {
                Debug.Log("All nodes rendered");
                AddSceneObjectComponentsRecursively(GameObject.Find("/Scene"));
            });
            yield return new WaitUntil(() => cycleTask.IsCompleted);

            //m_sceneReceived?.Invoke(this, new DataReceivedEventArgs(task.Result));
        }

        void AddLightSceneObjects(List<GameObject> lightObjects)
        {
            var sceneManager = core.getManager<SceneManager>();

            foreach (var obj in lightObjects)
            {
                SceneObject sceneObject = null;
                var light = obj.GetComponent<Light>();
                if (light.type == LightType.Spot)
                    sceneObject = SceneObjectSpotLight.Attach(obj, 0);
                else if (light.type == LightType.Directional)
                    sceneObject = SceneObjectDirectionalLight.Attach(obj, 0);
                else if (light.type == LightType.Point)
                    sceneObject = SceneObjectPointLight.Attach(obj, 0);
                obj.tag = "editable";
                sceneManager.sceneLightList.Add((SceneObjectLight)sceneObject);
                obj.transform.SetParent(m_streamingState.RootTransform);
            }
        }

        void AddCameraSceneObjects(List<GameObject> cameraObjects)
        {
            var sceneManager = core.getManager<SceneManager>();
            foreach(var obj in cameraObjects)
            {
                SceneObject sceneObject = null;
                sceneObject = SceneObjectCamera.Attach(obj, 0);
                obj.tag = "editable";
                obj.AddComponent<BoxCollider>();
                sceneManager.sceneCameraList.Add((SceneObjectCamera)sceneObject);
                obj.transform.SetParent(m_streamingState.RootTransform);
            }
        }


        void AddSceneObjectComponentsRecursively(GameObject parent)
        {
            var sceneManager = core.getManager<SceneManager>();
            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.GetComponent<SceneObject>() == null
                    && child.gameObject.GetComponent<MeshFilter>() != null)
                {
                    child.gameObject.tag = "editable";
                    child.gameObject.AddComponent<MeshCollider>();
                    var sceneObject = SceneObject.Attach(child.gameObject, 0);
                    sceneManager.simpleSceneObjectList.Add(sceneObject);
                    continue;
                }

                AddSceneObjectComponentsRecursively(child.gameObject);
            }
        }

        //! 
        //! Function that triggers the scene receiving process.
        //! @param ip The IP address to the server.
        //! @param port The port the server uses to send out the scene data.
        //! 
        public void receiveScene(string ip, string port)
        {
            start(ip, port);
        }


        private async Task StartNewCycle(Action downloadCallback, Action renderCallback)
        {
            m_streamingCycle = new StreamingCycle(downloadCallback);
            m_streamingCycle.OnRendered += () => renderCallback();
            AddCameraSceneObjects(m_streamingCycle.GetCameraObjects());
            AddLightSceneObjects(m_streamingCycle.GetLightObjects());
            await m_streamingCycle.Execute();
            var root = GameObject.Find("/Scene");
            AddSceneObjectComponentsRecursively(root);
        }
        public void ReceiveSceneUsingQr(object o, string ip)
        {
            core.getManager<UIManager>().hideMenu();
            receiveScene(ip, "5555");
        }

        protected override void run()
        {
        }
    }

}
