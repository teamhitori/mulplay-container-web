using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Models
{
    public record GameConfig(
        string gameName,
        double intervalMs,
        string frontEndLogic = LogicDefaults.frontEndLogic,
        string onStartLogic = LogicDefaults.onStartLogic,
        string onNewUserLogic = LogicDefaults.onNewUserLogic,
        string onUserEventLogic = LogicDefaults.onUserEventLogic,
        string onStepLogic = LogicDefaults.onStepLogic
        );

    static class LogicDefaults
    {

        public const string frontEndLogic = @"
// engine: BABYLON.Engine
// canvas: HTMLCustomElement
// This creates a basic Babylon Scene object (non-mesh)
var scene = new BABYLON.Scene(engine);

// This creates and positions a free camera (non-mesh)
var camera = new BABYLON.FreeCamera(""camera1"", new BABYLON.Vector3(0, 5, -10), scene);

// This targets the camera to scene origin
camera.setTarget(BABYLON.Vector3.Zero());

// This attaches the camera to the canvas
camera.attachControl(canvas, true);

// This creates a light, aiming 0,1,0 - to the sky (non-mesh)
var light = new BABYLON.HemisphericLight(""light"", new BABYLON.Vector3(0, 1, 0), scene);

// Default intensity is 1. Let's dim the light a small amount
light.intensity = 0.7;

// Our built-in 'sphere' shape.
var sphere = BABYLON.MeshBuilder.CreateSphere(""sphere"", {diameter: 2, segments: 32}, scene);

// Move the sphere upward 1/2 its height
sphere.position.y = 1;

// Our built-in 'ground' shape.
var ground = BABYLON.MeshBuilder.CreateGround(""ground"", {width: 6, height: 6}, scene);

return scene;";

        public const string onNewUserLogic = @"
// on New User Logic
// gameState
// userList
// newUserEventData
  ";

        public const string onStartLogic = @"
// on start logic
// gameState
// userList
  ";

        public const string onStepLogic = @"
// on step logic
// gameState
// userList";

        public const string onUserEventLogic = @"
// on user event logic
// gameState
// userList
// userEventData";


    }
}
