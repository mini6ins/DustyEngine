#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in vec3 aNormal;

uniform mat4 uProjectionMatrix;
uniform mat4 uModelViewMatrix;
uniform vec3 uObjectPosition;
uniform vec3 uObjectScale;

struct Material {
    vec3 ambient;        // Ambient color for the material
    sampler2D diffuse;   // Texture unit 0
    sampler2D specular;  // Texture unit 1
    vec3 specularColor;  // Fallback specular color if no texture
    float shininess;     // Shininess coefficient
};

struct DirLight {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;
    float constant;
    float linear;
    float quadratic;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float constant;
    float linear;
    float quadratic;
};

uniform vec3 viewPos;
uniform DirLight dirLight;
#define NR_POINT_LIGHTS 4
uniform PointLight pointLights[NR_POINT_LIGHTS];
uniform SpotLight spotLight;
uniform Material material;

out vec4 vColor;
out vec2 vTexCoord;
out vec3 FragPos;
out vec3 Normal;

void main()
{
    // Apply scaling and translation to position
    vec3 transformedPosition = aPosition * uObjectScale + uObjectPosition;
    FragPos = transformedPosition;
    
    // Scale the normal (only direction is important)
    Normal = aNormal * sign(uObjectScale);
    
    gl_Position = uProjectionMatrix * uModelViewMatrix * vec4(transformedPosition, 1.0);
    vColor = aColor;
    vTexCoord = aTexCoord;
}