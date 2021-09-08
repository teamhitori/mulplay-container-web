using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public static class LogicDefaults
    {

        public const string FrontendLogic =
@"// engine: BABYLON.Engine
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

frontendApi.enterGame();

frontendApi.onGameLoop((state) => {
	console.log(`From onGameLoop`, state);
});

frontendApi.onGameStop((state) => {
    console.log(`From onGameStop`, state);
});

frontendApi.onUserEvent((state) => {
    console.log(`From onUserEvent`, state);
});


engine.runRenderLoop(() => {
    scene.render();
});
";

        public const string BackendLogic =
@"//var intervalMs = gameConfig.intervalMs
//var fillScreen = gameConfig.fillScreen
//var screenRatio = gameConfig.screenRatio
//var sphere = new BABYLON.MeshBuilder.CreateSphere(`ball`, { diameter: 100, segments: 32 }, scene);

logger.log(`On Game Init`);

var gameState = {}

backendApi.onUserEvent((userPosition, state) => {
    logger.log(`From onUserEvent, Player-${userPosition}`);
});

backendApi.onGameLoop(() => {
    logger.log(`From onGameLoop`);
	backendApi.pushGameState(gameState);
});

backendApi.onUserEnter(userPosition => {
    logger.log(`From onUserEnter, Player-${userPosition}`);
	backendApi.pushUserState(userPosition, `Hello Player-${userPosition}`);
});

backendApi.onUserExit(userPosition => {
    logger.log(`From onUserExit`);
});

backendApi.onGameStop(() => {
    logger.log(`From onGameStop`);
});

backendApi.onGameStart(() => {
    logger.log(`From onGameStart`);
});
  ";

    }
}
