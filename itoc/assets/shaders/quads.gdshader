shader_type spatial;
//render_mode cull_disabled;

#include "quads.gdshaderinc"
#include "texture_buffer.gdshaderinc"

//global uniform sampler2D block_atlas : source_color, filter_nearest_mipmap;
//global uniform ivec2 block_atlas_count;

global uniform sampler2DArray block_textures : source_color, filter_nearest_mipmap;

uniform usampler2D texture_buff;
uniform int texture_buff_width;

varying float texture_id;

void vertex() {
	int vertex_index = QUAD_INDICES_MAP[VERTEX_ID % 6];

	uvec2 raw_quad_data = unpack_ulong(texture_buff, texture_buff_width, VERTEX_ID / 6);
	QuadData quad_data = parse_quad_data(raw_quad_data.x, raw_quad_data.y);

	VERTEX = get_quad_vertex(quad_data, vertex_index);
	UV = QUAD_UVS[vertex_index];
	NORMAL = QUAD_NORMS[quad_data.type];
	
	texture_id = float(quad_data.texture_index);
}

void fragment() {
	// uint value = texelFetch(texture_buff, ivec2(int(UV.x * 256.0), 0), 0).r;
	// float r = float(value & 0xFFu)/255.0;
	// float g = float(value >> 8u & 0xFFu) / 255.0;
	// float b = 0.0; //float(value & uint(0x0F00))/255.0;
	// ALBEDO = vec3(UV.x * NORMAL.z, UV.y * NORMAL.x, NORMAL.y * UV.x * UV.y);

	ALBEDO = texture(block_textures, vec3(UV, texture_id)).rgb;
}
