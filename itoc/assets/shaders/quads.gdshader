shader_type spatial;

#include "quads.gdshaderinc"
#include "texture_buffer.gdshaderinc"

global uniform sampler2DArray block_textures : source_color, filter_nearest_mipmap;

uniform usampler2D quads_buff;
uniform int quads_buff_width;

varying float texture_id;

void vertex() {
	int vertex_index = QUAD_INDICES_MAP[VERTEX_ID % 6];

	uvec2 raw_quad_data = unpack_ulong(quads_buff, quads_buff_width, VERTEX_ID / 6);
	QuadData quad_data = parse_quad_data(raw_quad_data.x, raw_quad_data.y);

	VERTEX = get_quad_vertex(quad_data, vertex_index);
	UV = QUAD_UVS[vertex_index];
	NORMAL = QUAD_NORMS[quad_data.type];

	texture_id = float(quad_data.texture_index);
}

void fragment() {
	ALBEDO = texture(block_textures, vec3(UV, texture_id)).rgb;
}
