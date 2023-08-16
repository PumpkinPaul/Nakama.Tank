# Nakama.Tank
TANK - networked multiplayer with client prediction.

## Overview

A short demo project of the Atari classic, TANK - highlighting two core concepts:
- A client relayed networked multiplayer element
- Network prediction

### Network Multiplayer

Implemented using a Client Relayed Multiplayer approach using [Nakama](https://heroiclabs.com/nakama/) server and client libraries.

> Nakama is the leading open source game server framework for building online multiplayer games in Godot, Unity, Unreal Engine, MonoGame, LibGDX, Defold, Cocos2d, Phaser, Macroquad and more

### Network Prediction

> This project shows how to use prediction and smoothing algorithms to compensate for network lag. This makes remotely controlled objects appear to move smoothly even when there is a significant delay in packets being delivered over the network. 

## Credits

Inspired by:
- [Tank](https://en.wikipedia.org/wiki/Combat_(video_game)) by Atari

Frameworks:
- [FNA](https://github.com/FNA-XNA/FNA) - _an XNA4 reimplementation that focuses solely on developing a fully accurate XNA4 runtime for the desktop._
- [Nakama](https://heroiclabs.com/nakama/) _by Herioc Labs_
- [XNA Network Prediction Sample](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/NetworkPredictionSample_4_0) _by Microsoft_
