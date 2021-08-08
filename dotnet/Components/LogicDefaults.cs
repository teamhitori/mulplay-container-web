using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public static class LogicDefaults
    {

        public const string FrontEndLogic = @"
// engine: BABYLON.Engine
// canvas: HTMLCustomElement
// This creates a basic Babylon Scene object (non-mesh)
var scene = new BABYLON.Scene(engine);

// This creates and positions a free camera (non-mesh)
var camera = new BABYLON.FreeCamera(`camera1`, new BABYLON.Vector3(0, 5, -10), scene);

// This targets the camera to scene origin
camera.setTarget(BABYLON.Vector3.Zero());

// This attaches the camera to the canvas
camera.attachControl(canvas, true);

// This creates a light, aiming 0,1,0 - to the sky (non-mesh)
var light = new BABYLON.HemisphericLight(`light`, new BABYLON.Vector3(0, 1, 0), scene);

// Default intensity is 1. Let's dim the light a small amount
light.intensity = 0.7;

// Our built-in 'sphere' shape.
var sphere = BABYLON.MeshBuilder.CreateSphere(`sphere`, {diameter: 2, segments: 32}, scene);

// Move the sphere upward 1/2 its height
sphere.position.y = 1;

// Our built-in 'ground' shape.
var ground = BABYLON.MeshBuilder.CreateGround(`ground`, {width: 6, height: 6}, scene);

gameLoopApi.onGameState((state) => {
	console.log(`From onGameState`, state);
});

gameLoopApi.onStopState((state) => {
    console.log(`From onStopState`, state);
});

gameLoopApi.onUserEnterState((state) => {
    console.log(`From onUserEnterState`, state);
});

gameLoopApi.onUserExitState((state) => {
    console.log(`From onUserExitState`, state);
});

gameLoopApi.onUserState((state) => {
    console.log(`From onUserState`, state);
});


return scene;
";

        public const string UserEnterLogic = @"
// on New User Logic
// scene
// gameState
// eventData
  ";

        public const string UserExitLogic = @"
// on New User Logic
// scene
// gameState
// eventData
  ";

        public const string StartLogic = @"
// on start logic
// scene
// gameState
 ";

        public const string GameLoopLogic = @"
// on step logic
// scene
// gameState
";

        public const string UserEventLogic = @"
// on user event logic
// scene
// gameState
// eventData";


    }
}
