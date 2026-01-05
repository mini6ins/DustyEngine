#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec3 aNormal;

uniform mat4 uProjectionMatrix;
uniform mat4 uModelViewMatrix;
uniform vec3 uObjectPosition;
uniform vec3 uObjectScale;

out vec4 vColor;

void main()
{
    vec3 scaledPosition = aPosition * uObjectScale;
    vec4 worldPosition = vec4(scaledPosition + uObjectPosition, 1.0);
    gl_Position = uProjectionMatrix * uModelViewMatrix * worldPosition;
    vColor = aColor;
}
