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

//! @file "SceneCreatorModule.cs"
//! @brief implementation of TRACER scene creator module
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 03.08.2022

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

//using UnityEditor.Animations;
using UnityEngine;
using static tracer.AbstractParameter;

namespace tracer
{

    public class SceneGLTFCreatorModule : SceneManagerModule
    {
 


        //!
        //! Constructor
        //! Creates an reference to the network manager and connects the scene creation method to the scene received event in the network requester.
        //!
        //! @param name Name of this module
        //! @param core Reference to the TRACER core
        //!
        public SceneGLTFCreatorModule(string name, Manager manager) : base(name, manager)
        {
            //if (core.isServer)
            //    load = false;
        }

        //!
        //! Cleaning up event registrations.
        //!
        protected override void Cleanup(object sender, EventArgs e)
        {
            base.Cleanup(sender, e);

            NetworkManager networkManager = core.getManager<NetworkManager>();
            SceneGLTFReceiverModule sceneReceiverModule = networkManager.getModule<SceneGLTFReceiverModule>();
            SceneGLTFStorageModule sceneStorageModule = manager.getModule<SceneGLTFStorageModule>();
            if (sceneReceiverModule != null)
                sceneReceiverModule.m_sceneReceived -= CreateScene;
            sceneStorageModule.sceneLoaded -= CreateScene;
        }

        //!
        //! Init function of the module.
        //!
        protected override void Init(object sender, EventArgs e)
        {
            NetworkManager networkManager = core.getManager<NetworkManager>();
            SceneGLTFReceiverModule sceneGLTFReceiverModule = networkManager.getModule<SceneGLTFReceiverModule>();
            SceneGLTFStorageModule sceneStorageModule = manager.getModule<SceneGLTFStorageModule>();
            if (sceneGLTFReceiverModule != null)
                sceneGLTFReceiverModule.m_sceneReceived += CreateScene;
            sceneStorageModule.sceneLoaded += CreateScene;
        }

        //!
        //! Function that creates the Unity scene content.
        //!
        public void CreateScene(object o, EventArgs e)
        {
            manager.ResetScene();


            manager.emitSceneReady();
        }
    }
}
