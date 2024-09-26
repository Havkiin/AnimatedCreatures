# Animated Creatures

An animated fish created using a combination of procedural animation in Unity (C#) and ShaderLab for its display.

## Generation

The [Spine Generator](Assets/Scripts/SpineGenerator.cs) script creates all the joints in the spine of the fish.

## Behaviour

The [Movement Component](Assets/Scripts/MovementComponent.cs) scripts then handles the movement, combining steering behaviours (Wander by default) with a sine function to emulate wave-like movement.

## Display

Vertex positions are passed from the Spine Generator to the [Fish Shader](Assets/Shaders/Fish.shader). In the fragment shader, the fish's outline is drawn using Catmull-Rom interpolation, while the colors are determined by point-in-polygon and point-in-ellipse algorithms.
