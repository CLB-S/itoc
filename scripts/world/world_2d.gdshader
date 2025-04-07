shader_type spatial;

// Heightmap texture (grayscale)
uniform sampler2D heightmap;
// Height scaling factor
uniform float height_scale = 1.0;
// Texture tiling
uniform vec2 texture_scale = vec2(1.0, 1.0);

void vertex() {
    // Sample heightmap and adjust vertex position
    float height = texture(heightmap, UV * texture_scale).r;
    VERTEX.y += height * height_scale;
}

void fragment() {
    // Calculate normal from heightmap
    float step = 0.01; // Adjust this based on your texture resolution
    
    // Sample neighboring heights
    float h_left = texture(heightmap, UV * texture_scale - vec2(step, 0.0)).r;
    float h_right = texture(heightmap, UV * texture_scale + vec2(step, 0.0)).r;
    float h_down = texture(heightmap, UV * texture_scale - vec2(0.0, step)).r;
    float h_up = texture(heightmap, UV * texture_scale + vec2(0.0, step)).r;
    
    // Calculate normal using Sobel filter or simple differences
    vec3 normal;
    normal.x = (h_left - h_right);
    normal.z = (h_down - h_up);
    normal.y = 2.0 * step; // This controls the steepness of the normal
    
    // Normalize and set the normal
    NORMAL = normalize(normal);
    
    // Output grayscale color based on height
    float height = texture(heightmap, UV * texture_scale).r;
    ALBEDO = vec3(height);
    METALLIC = 0.0;
    ROUGHNESS = 1.0;
}