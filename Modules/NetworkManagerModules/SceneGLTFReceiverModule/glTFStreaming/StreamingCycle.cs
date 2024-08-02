using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using GLTFast;
using GLTFast.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Extensions;
using Debug = UnityEngine.Debug;

namespace tracer
{
    /// <summary>
    /// A streaming cycle represents a series of steps for streaming a binary glTF 2.0 resource.
    /// <para>The following steps are performed asynchronously:</para>
    /// <list type="number">
    /// <item>Node selection</item>
    /// <item>Downloading of data for selected nodes</item>
    /// <item>Creation of new GLB data</item>
    /// <item>Rendering of the GLB data</item>
    /// </list>
    /// Since a <c>StreamingCycle</c> reads from and writes to a thread-unsafe <c>StreamingState</c>, more than one <c>StreamingCycle</c>
    /// should not execute at the same time.
    /// </summary>
    public class StreamingCycle
    {
        private StreamingState _state;
        private GLBDownloader _downloader;
        private HashSet<ExtendedNode> _processedNodes;
        private GLTFastRenderer _renderer;
        private NodePruner _nodePruner;
        private Action _downloadCallback;
        private bool _isBusy = false;
        private Budgeter<Budgetable<ExtendedNode>> _budgeter;
        private ConcurrentDictionary<string, string> _renderedNodeNames;
        private List<GameObject> _cameraObjects = new List<GameObject>();
        private List<GameObject> _lightObjects = new List<GameObject>();
        public delegate void OnRenderedHandler();
        public event OnRenderedHandler OnRendered;


        public StreamingCycle(Action onRenderedCallback = null)
        {
            _state = StreamingState.Instance;
            _downloader = new GLBDownloader(_state.ResourceUri);
            _downloadCallback = onRenderedCallback;
            _renderedNodeNames = new ConcurrentDictionary<string, string>();
            _budgeter = new Budgeter<Budgetable<ExtendedNode>>();
            _processedNodes = new HashSet<ExtendedNode>();
            _nodePruner = new NodePruner(_state.GLTFRoot);
            _state.Nodes = new List<ExtendedNode>();

            var geo = new GameObject("Geo");
            geo.transform.SetParent(_state.RootTransform, false);
            _renderer = new GLTFastRenderer(geo.transform);

            InitializeNodes();
        }



        #region Initialization

        private float GetMaxDistance()
        {
            var maxPos = 0f;
            foreach (var node in _state.GLTFRoot.Nodes)
            {
                if (node.Mesh == null) continue;
                foreach (var prim in node.Mesh.Value.Primitives)
                {
                    if (!prim.Attributes.ContainsKey("POSITION"))
                        continue;
                    var accessor = prim.Attributes["POSITION"].Value;
                    Vector3 accessorMax = new Vector3((float)accessor.Max[0], (float)accessor.Max[1], (float)accessor.Max[2]);
                    var distance = Vector3.Distance(UnityEngine.Camera.main.transform.position, accessorMax);
                    if (distance > maxPos)
                        maxPos = distance;
                }
            }
            return maxPos;
        }

        private void InitializeNodes()
        {
            Stopwatch sw = Stopwatch.StartNew();

            foreach (var node in _state.GLTFRoot.Nodes)
            {
                if (node.Name == null)
                    node.Name = Guid.NewGuid().ToString();



                // Handle camera nodes

                if (node.Camera != null)
                {
                    AddCameraFromNode(node);
                    continue; // Assuming a camera node isn't also a light node
                }

                if (node.Extensions == null || node.Extensions["KHR_lights_punctual"] == null) continue;

                // Handle light nodes
                var extensions = node.Extensions["KHR_lights_punctual"] as KHR_LightsPunctualNodeExtension;
                var khronosLight = extensions.LightId.Value;
                var gltFastLight = ConvertKhronosToGltFastLight(khronosLight);
                GameObject lightObject = new GameObject(gltFastLight.name ?? "Light");
                var unityLight = lightObject.AddComponent<Light>();
                gltFastLight.ToUnityLight(unityLight, 1);
                ApplyTransform(lightObject, node);
                _lightObjects.Add(lightObject);
            }

            foreach (var scene in _state.GLTFRoot.Scenes)
            {
                if (_state.NoLevelOfDetail)
                {
                    // First make copies of nodes without level of detail
                    foreach (var node in scene.Nodes.ToList())
                    {
                        node.Value.Extensions = null;
                        NodeCopier copier = new NodeCopier(node.Value, _state.GLTFRoot);
                        copier.CopyWithoutLOD(GetMaxDistance());
                    }
                }

                foreach (var node in scene.Nodes)
                    InitializeNode(node);
                var nodeIdsWithoutLights = scene.Nodes.Where(nodeId =>
                {
                    var node = _state.GLTFRoot.Nodes[nodeId.Id];
                    return node.Extensions == null || !node.Extensions.ContainsKey(KHR_lights_punctualExtensionFactory.EXTENSION_NAME);
                }).ToList();

                // Replace the scene's nodes with the filtered list
                scene.Nodes = nodeIdsWithoutLights;
                scene.Extensions = null;
            }


            _state.GLTFRoot.Extensions = null;
            _state.GLTFRoot.ExtensionsRequired = null;
            _state.GLTFRoot.ExtensionsUsed = null;

            sw.Stop();
            Debug.Log($"initializing and copying took {sw.ElapsedMilliseconds}ms");
        }


        public List<GameObject> GetCameraObjects()
        {
            return _cameraObjects;
        }

        public List<GameObject> GetLightObjects()
        {
            return _lightObjects;
        }

        private void AddCameraFromNode(GLTF.Schema.Node node)
        {
            var gltfCamera = node.Camera.Value;
            node.GetUnityTRSProperties(out var pos, out var rot, out var scale);

            var camObject = new GameObject(node.Name ?? "Camera");
            var unityCamera = camObject.AddComponent<UnityEngine.Camera>();

            if (gltfCamera.Type == GLTF.Schema.CameraType.perspective)
            {
                unityCamera.orthographic = false;
                unityCamera.fieldOfView = (float)(gltfCamera.Perspective.YFov * Mathf.Rad2Deg);
                unityCamera.nearClipPlane = (float)gltfCamera.Perspective.ZNear;
                unityCamera.farClipPlane = (float)gltfCamera.Perspective.ZFar;
            }
            else if (gltfCamera.Type == GLTF.Schema.CameraType.orthographic)
            {
                unityCamera.orthographic = true;
                unityCamera.orthographicSize = (float)gltfCamera.Orthographic.YMag;
                unityCamera.nearClipPlane = (float)gltfCamera.Orthographic.ZNear;
                unityCamera.farClipPlane = (float)gltfCamera.Orthographic.ZFar;
            }

            // Apply the node's transformations
            camObject.transform.position = pos;
            camObject.transform.rotation = rot * Quaternion.Euler(0, 180, 0);
            camObject.transform.localScale = scale;


            _cameraObjects.Add(camObject);
        }

        private void ApplyTransform(GameObject lightObject, GLTF.Schema.Node node)
        {
            node.GetUnityTRSProperties(out var pos, out var rot, out var scale);
            lightObject.transform.position = pos;
            lightObject.transform.rotation = rot * Quaternion.Euler(0, 180, 0);
            lightObject.transform.localScale = scale;
        }

        private LightPunctual ConvertKhronosToGltFastLight(PunctualLight light)
        {
            var gltFastLight = new LightPunctual()
            {
                LightColor = light.Color.ToUnityColorRaw(),
                intensity = (float)light.Intensity / 1000f,
                name = light.Name,
                range = (float)light.Range,
                spot = new SpotLight()
                {
                }

            };
            if (light.Type == GLTF.Schema.KHR_lights_punctual.LightType.directional)
                gltFastLight.SetLightType(LightPunctual.Type.Directional);
            else if (light.Type == GLTF.Schema.KHR_lights_punctual.LightType.spot)
                gltFastLight.SetLightType(LightPunctual.Type.Spot);
            else if (light.Type == GLTF.Schema.KHR_lights_punctual.LightType.point)
                gltFastLight.SetLightType(LightPunctual.Type.Point);
            return gltFastLight;
        }


        private void InitializeNode(NodeId nodeId)
        {
            var progress = new NodeProgress(_state, nodeId.Value);
            var node = new ExtendedNode(nodeId.Value, _state);
            _state.Nodes.Add(node);

            _state.NodeBoundingBoxes.Add(node, new NodeBoundingBox(_state.GLTFRoot, node.OriginalNode));

            if (progress.GetRanges().Count == 0) // Some nodes are hierarchical and have no mesh to download
            {
                node.Progress.TrackAllRanges(DownloadStatus.Completed);
                node.Progress.RenderStatus = RenderStatus.Fully;
            }

            // Handle cameras
            if (node.OriginalNode.Camera != null)
            {
                var cameraId = node.OriginalNode.Camera.Id;
                var camera = _state.GLTFRoot.Cameras[cameraId];
                Debug.Log("Camera found: " + camera.Name);
                // Additional camera handling logic can be placed here
            }

            // Recursively initialize children nodes
            if (nodeId.Value.Children != null)
            {
                foreach (var child in nodeId.Value.Children)
                {
                    InitializeNode(child);
                }
            }
        }


        #endregion

        #region Public Methods
        private List<ExtendedNode> SelectNodes()
        {
            float percentage = _renderedNodeNames.Count / (float)_state.Nodes.Count * 100f;
            double desiredTimeSeconds = _state.NetworkSettings.Patience / 1000.0;
            double bytesToDownload = (desiredTimeSeconds * 1000000 / 8) * _state.AverageDownloadSpeed;
            var budget = bytesToDownload == 0 ? 1000000 : (long)bytesToDownload;
            var factory = new BudgetableNodeFactory();
            var budgetables = factory.CreateBudgetables(_state.Nodes.Where(n => n.Progress.DownloadStatus != DownloadStatus.Completed).ToList());
            _budgeter.Budget = budget;
            _budgeter.RemainingBudget = budget;
            _budgeter.SetBudgetables(budgetables);
            var nodesInBudget = _budgeter.AssignBudget();

            return nodesInBudget.Select(n => n.BudgetableObj).ToList();
        }


        public async Task Execute()
        {
            var nodeSelection = SelectNodes();
            if (nodeSelection.Count == 0 || HasRenderedAllNodes())
                return;
            _processedNodes.AddRange(nodeSelection);

            await DownloadAndCopy(nodeSelection);
            _state.AverageDownloadSpeed = GLBDownloader.SpeedTracker.CalculateAverageSpeed();
            Debug.Log($"Average download speed: {_state.AverageDownloadSpeed} mbps");
            var nodes = _state.Nodes
                .Where(n =>
                n.Progress.DownloadStatus == DownloadStatus.Completed
                && n.Progress.RenderStatus == RenderStatus.Not
                && !_renderedNodeNames.ContainsKey(n.OriginalNode.Name))
                .ToList();

            Task _ = TryRender(nodes);
            _downloadCallback();
        }

        public async Task TryRender(List<ExtendedNode> nodes)
        {
            foreach (var node in nodes)
                _renderedNodeNames.TryAdd(node.OriginalNode.Name, node.OriginalNode.Name);
            Debug.Log("Nodes to render in cycle: " + nodes.Count);
            var newRoot = _nodePruner.Prune(nodes.Select(n => n.OriginalNode).ToList());
            var json = await JsonUtils.SerializeAsync(newRoot);
            var builder = new GLBBuilder(json, _state.GLBFile.Data);
            var bytes = builder.BuildGLBBytesAsync();
            await _renderer.RenderAsync(bytes);
            // HideObjectsRecursive(_state.RootTransform, nodes.Select(n => n.OriginalNode.Name).ToList());

            foreach (var node in nodes)
                node.Progress.RenderStatus = RenderStatus.Fully;
            OnRendered?.Invoke();
        }


        private async Task<IResponse> DownloadAndCopy(List<ExtendedNode> nodes)
        {
            if (nodes.Count == 0)
                return null;
            var rangesToDownload = GetUndownloadedRanges(nodes);

            var response = await _downloader.DownloadGLBAsync(rangesToDownload);
            await ProcessResponse(response);
            return response;
        }


        public bool IsBusy() => _isBusy;


        public bool HasRenderedAllNodes()
        {
            return _state.Nodes.All(n => n.Progress.RenderStatus == RenderStatus.Fully);
        }

        #endregion

        #region Private Methods



        private List<ByteRange> GetUndownloadedRanges(List<ExtendedNode> nodes)
        {
            var ranges = new List<ByteRange>();
            foreach (var node in nodes)
            {
                ranges.AddRange(node.Progress.GetRangesOfStatus(DownloadStatus.Unbegun));
            }

            return ranges.Distinct().ToList();
        }


        private void HideObjectsRecursive(Transform parent, List<string> names)
        {
            foreach (Transform child in parent)
            {
                var newName = child.gameObject.name.Replace("_noLOD", "");
                if (child.gameObject.name.EndsWith("_noLOD")
                    && names.Contains(newName))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                // Recursively call the function for the current child to continue traversal
                HideObjectsRecursive(child, names);
            }
        }
        private async Task ProcessResponse(IResponse response)
        {
            await Task.Run(() =>
            {
                if (response.ResponseType == ResponseType.Complete)
                    ProcessCompleteResponse();
                else if (response.ResponseType == ResponseType.Multiple)
                    ProcessMultipartResponse();
                else if (response.ResponseType == ResponseType.Single)
                    ProcessSinglePartResponse();

                #region Response Processing Methods
                void ProcessCompleteResponse()
                {
                    var completeResponse = (CompleteResponse)response;
                    var gltfOffset = 20 + (int)_state.GLBFile.JSONSubheader.ChunkLength + 8;
                    var data = completeResponse.GetData();
                    foreach (var node in _state.Nodes)
                        node.Progress.TrackAllRanges(DownloadStatus.Completed);
                    Array.Copy(data, gltfOffset, _state.GLBFile.Data, 0, data.Length - gltfOffset);
                }

                void ProcessMultipartResponse()
                {
                    var multipleResponse = (MultipartResponse)response;
                    foreach (var part in multipleResponse.Parts)
                    {
                        var range = new ByteRange(part.ContentRange.From.Value, part.ContentRange.To.Value);
                        foreach (var node in _state.Nodes)
                            node.Progress.TrackRange(range, DownloadStatus.Completed);
                        CopyReceivedData(part.Data.ToArray(), range);
                    }
                }

                void ProcessSinglePartResponse()
                {
                    var singleResponse = (SinglePartResponse)response;
                    var range = new ByteRange(singleResponse.ContentRange.From.Value, singleResponse.ContentRange.To.Value);
                    foreach (var node in _state.Nodes)
                        node.Progress.TrackRange(range, DownloadStatus.Completed);
                    CopyReceivedData(singleResponse.GetData(), range);
                }
                #endregion
            });
        }

        private void CopyReceivedData(byte[] data, ByteRange range)
        {
            var gltfOffset = 20 + (int)_state.GLBFile.JSONSubheader.ChunkLength + 8;
            Array.Copy(data, 0, _state.GLBFile.Data, range.StartByte - gltfOffset, data.Length);
        }
        #endregion
    }
}
