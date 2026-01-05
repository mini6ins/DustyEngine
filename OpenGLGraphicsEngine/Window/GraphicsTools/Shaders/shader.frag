#version 330 core

in vec4 vColor;
in vec2 vTexCoord;
in vec3 FragPos;
in vec3 Normal;

out vec4 FragColor;

struct Material {
    vec3 ambient;        // Добавлен ambient цвет материала
    sampler2D diffuse;
    sampler2D specular;
    vec3 specularColor;  // Добавлен specularColor для случая отсутствия текстуры
    float shininess;
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

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main()
{
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);

    // Phase 1: Directional lighting
    vec3 result = CalcDirLight(dirLight, norm, viewDir);

    // Phase 2: Point lights
    for(int i = 0; i < NR_POINT_LIGHTS; i++)
        result += CalcPointLight(pointLights[i], norm, FragPos, viewDir);

    // Phase 3: Spot light
    result += CalcSpotLight(spotLight, norm, FragPos, viewDir);

    vec4 texColor = texture(material.diffuse, vTexCoord);
    FragColor = texColor * vec4(result, 1.0);
}

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    // Combine results
    vec3 ambient = light.ambient * (material.ambient + vec3(texture(material.diffuse, vTexCoord)));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, vTexCoord));
    vec3 specular = light.specular * spec * 
        (textureSize(material.specular, 0).x > 0 ? 
            vec3(texture(material.specular, vTexCoord)) : 
            material.specularColor);
    
    return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    // Attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
                light.quadratic * (distance * distance));
    
    // Combine results
    vec3 ambient = light.ambient * (material.ambient + vec3(texture(material.diffuse, vTexCoord)));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, vTexCoord));
    vec3 specular = light.specular * spec * 
        (textureSize(material.specular, 0).x > 0 ? 
            vec3(texture(material.specular, vTexCoord)) : 
            material.specularColor);
    
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    
    return (ambient + diffuse + specular);
}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    
    // Diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    
    // Specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    // Attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + 
                light.quadratic * (distance * distance));
    
    // Spotlight intensity
    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    
    // Combine results
    vec3 ambient = light.ambient * (material.ambient + vec3(texture(material.diffuse, vTexCoord)));
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, vTexCoord));
    vec3 specular = light.specular * spec * 
        (textureSize(material.specular, 0).x > 0 ? 
            vec3(texture(material.specular, vTexCoord)) : 
            material.specularColor);
    
    ambient *= attenuation;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    
    return (ambient + diffuse + specular);
}