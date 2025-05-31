const int[6] QUAD_INDICES_MAP = {
	0,1,2,0,2,3
};

const vec2[4] QUAD_UVS = {
	vec2(0,1),
	vec2(0,0),
	vec2(1,0),
	vec2(1,1)
};

#define INV_SQRT_2 0.7071067811865475

const vec3[12] QUAD_NORMS = {
	vec3(0,1,0),  // +y
	vec3(0,-1,0), // -y
	vec3(1,0,0),  // +x
	vec3(-1,0,0), // -x
    vec3(0,0,1),  // +z
	vec3(0,0,-1), // -z
    vec3(INV_SQRT_2,0,INV_SQRT_2),   // +xz
    vec3(-INV_SQRT_2,0,-INV_SQRT_2), // -xz
    vec3(0,INV_SQRT_2,INV_SQRT_2),   // +yz
    vec3(0,-INV_SQRT_2,-INV_SQRT_2), // -yz
    vec3(INV_SQRT_2,INV_SQRT_2,0),   // +xy
    vec3(-INV_SQRT_2,-INV_SQRT_2,0)  // -xy
};

#undef INV_SQRT_2

const vec3[48] QUAD_VERTICES = {
    // +y
    vec3(0,0,1),
    vec3(0,0,0),
    vec3(1,0,0),
    vec3(1,0,1),
    // -y
    vec3(1,0,1),
    vec3(1,0,0),
    vec3(0,0,0),
    vec3(0,0,1),
    // +x
    vec3(0,0,1),
    vec3(0,1,1),
    vec3(0,1,0),
    vec3(0,0,0),
    // -x
    vec3(0,0,0),
    vec3(0,1,0),
    vec3(0,1,1),
    vec3(0,0,1),
    // +z
	vec3(0,0,0),
	vec3(0,1,0),
	vec3(1,1,0),
	vec3(1,0,0),
	// -z
	vec3(1,0,0),
	vec3(1,1,0),
	vec3(0,1,0),
	vec3(0,0,0),

    // bevels //

    // +xz
	vec3(0,0,1),
	vec3(0,1,1),
	vec3(1,1,0),
	vec3(1,0,0),
    // -xz
	vec3(1,0,0),
	vec3(1,1,0),
	vec3(0,1,1),
	vec3(0,0,1),
    // +yz
    vec3(0,0,1),
    vec3(0,1,0),
    vec3(1,1,0),
    vec3(1,0,1),
    // -yz
    vec3(1,0,1),
    vec3(1,1,0),
    vec3(0,1,0),
    vec3(0,0,1),
    // +xy
    vec3(1,0,1),
    vec3(0,1,1),
    vec3(0,1,0),
    vec3(1,0,0),
    // -xy
    vec3(1,0,0),
    vec3(0,1,0),
    vec3(0,1,1),
    vec3(1,0,1)
};

struct QuadData {
    vec3 offset;
    int type; // 0 to 11 for +y, -y, +x, -x, +z, -z, +xz, -xz, +yz, -yz, +xy, -xy
    vec3 scale;
    vec3 offset_in_block;
	int texture_index;
};

const uint QUAD_DATA_LENGTH = 28u;

QuadData parse_quad_data(uint low, uint high) {
    QuadData quad_data;
    uvec3 offset = uvec3(0, 0, 0);
    offset.x = low & 0x3Fu;            // 6 bits for x
    offset.y = (low >> 6u) & 0x3Fu;    // 6 bits for y
    offset.z = (low >> 12u) & 0x3Fu;   // 6 bits for z
    quad_data.offset = vec3(offset);

    quad_data.type = int((low >> 18u) & 0xFu); // 4 bits for type

    vec3 scale = vec3(1.0, 1.0, 1.0);
    if ((low >> 22u & 1u) == 1u) {
        scale.x = 0.5; // Scale down for x
    }

    if ((low >> 23u & 1u) == 1u) {
        scale.y = 0.5; // Scale down for y
    }

    if ((low >> 24u & 1u) == 1u) {
        scale.z = 0.5; // Scale down for z
    }

    quad_data.scale = scale;

    vec3 offset_in_block = vec3(0.0, 0.0, 0.0);
    if ((low >> 25u & 1u) == 1u) {
        offset_in_block.x = 0.5; // Offset for x
    }

    if ((low >> 26u & 1u) == 1u) {
        offset_in_block.y = 0.5; // Offset for y
    }

    if ((low >> 27u & 1u) == 1u) {
        offset_in_block.z = 0.5; // Offset for z
    }

    quad_data.offset_in_block = offset_in_block;

	quad_data.texture_index = int((low >> 28u) | ((high & 0xFFu) << 4u));

    return quad_data;
}

vec3 get_quad_vertex(QuadData quad, int index) {
    return QUAD_VERTICES[quad.type * 4 + index] * quad.scale
        + quad.offset_in_block + quad.offset;
}

vec2 get_quad_uv(QuadData quad, int index, ivec2 atlas_count) {
    vec2 uv = QUAD_UVS[index];
    vec2 scaler = vec2(1.0 / float(atlas_count.x), 1.0 / float(atlas_count.y));
	uv *= 0.996 * scaler;
    uv += 0.002 * scaler; // Add a small offset to avoid seams
    int atlas_x = quad.texture_index % atlas_count.x;
    int atlas_y = quad.texture_index / atlas_count.x;
    uv.x += float(atlas_x) * scaler.x;
    uv.y += float(atlas_y) * scaler.y;
    return uv;
}