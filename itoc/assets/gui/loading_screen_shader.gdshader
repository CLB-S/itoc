shader_type canvas_item;

// Function to calculate the shortest distance from a point to a line segment
float segment(vec2 p, vec2 a, vec2 b) {
    p -= a;
    b -= a;
    return length(p - b * clamp(dot(p, b) / dot(b, b), 0.0, 1.0));
}

// Function to create a 2D rotation matrix for a given angle
mat2 rot(float a) {
    return mat2(vec2(cos(a), -sin(a)), vec2(sin(a), cos(a)));
}

// Function to transform a 3D point with rotation based on time
vec2 T(vec3 p, float t) {
    p.xy *= rot(-t);       // Rotate in the XY plane
    p.xz *= rot(0.785);    // Rotate in the XZ plane
    p.yz *= rot(-0.625);   // Rotate in the YZ plane
    return p.xy;           // Return the transformed XY coordinates
}

void fragment() {
    vec2 R = 1.0 / SCREEN_PIXEL_SIZE; // Screen resolution scaling factor
    vec2 u = 1.5 * SCREEN_UV * R;     // Scaled UV coordinates
    vec2 U = 10.0 * u / R.y;          // Adjusted UV coordinates for tiling
    vec2 M = vec2(2.0, 2.3);          // Tiling size
    vec2 I = floor(U / M) * M;        // Base tile position
    vec2 J;                           // Current tile position
    U = mod(U, M);                    // UV coordinates within the tile
    vec4 color = vec4(0.15, 0.15, 0.19, 1);               // Output color accumulator

    // Loop through 4 sub-tiles
    for (int k = 0; k < 4; k++) {
        vec2 X = vec2(float(k % 2), float(k / 2)) * M; // Offset for sub-tile
        J = I + X;                                     // Current sub-tile position
        if (int(J.x / M.x) % 2 > 0) {                 // Alternate row offset
            X.y += 1.15;
        }
		
        // Calculate wave transformation based on time
        float t = tanh(-0.2 * (J.x + J.y) + mod(2.0 * TIME, 15.0) - 1.6) * 0.785;

        // Draw a cube in the sub-tile
        for (float a = 0.0; a < 6.0; a += 1.57) { // Iterate over cube edges
            vec3 A = vec3(cos(a), sin(a), 0.7);  // Start point of edge
            vec3 B = vec3(-A.y, A.x, 0.7);       // End point of edge

            // Macro to draw a line segment with smooth edges
            #define L(A, B) color += 0.4 * smoothstep(15.0 / R.y, 0.0, segment(U - X, T(A, t), T(B, t)))

            L(A, B);                             // Draw edge
            L(A, A * vec3(1.0, 1.0, -1.0));      // Draw vertical edge
            A.z = -A.z;                          // Flip Z for bottom face
            B.z = -B.z;
            L(A, B);                             // Draw bottom edge

            #undef L
        }
    }

    COLOR = color; // Set the final color
}