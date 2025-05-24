shader_type spatial;
render_mode blend_add;
render_mode wireframe;
render_mode fog_disabled;
render_mode shadows_disabled;
render_mode cull_disabled;

uniform vec3 x_color : source_color = vec3(1.0, 0.0, 0.0);
uniform vec3 y_color : source_color = vec3(0.0, 1.0, 0.0);
uniform vec3 z_color : source_color = vec3(0.0, 0.0, 1.0);


void vertex() {
	// vec3 normal = normalize(NORMAL);

    // float x = abs(dot(normal, vec3(1.0, 0.0, 0.0)));
    // float y = abs(dot(normal, vec3(0.0, 1.0, 0.0)));
    // float z = abs(dot(normal, vec3(0.0, 0.0, 1.0)));

    // vec3 color;
    // if (x > y && x > z) {
    //     color = x_color;
    // } else if (y > x && y > z) {
    //     color = y_color;
    // } else {
    //     color = z_color;
    // }

    // float intensity = max(max(x, y), z);

	// COLOR = vec4(color * intensity, 1.0);
}

void fragment() {
	// Called for every pixel the material is visible on.

    // ALBEDO = COLOR.xyz;
    ALBEDO = vec3(UV, 0.7);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
