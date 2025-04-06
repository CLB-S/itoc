shader_type spatial;

void vertex() {
	VERTEX.y += 0.3 * cos(VERTEX.x * 4.0) * sin(VERTEX.z * 4.0);
}

void fragment() {
  NORMAL_MAP = vec3(cos(VERTEX.x * 4.0),2,3);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
