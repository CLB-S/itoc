// Original: MMqd/godot-nishita-sky-with-volumetric-clouds
// Fully rewritten here in ITOC.

shader_type sky;
render_mode use_half_res_pass, use_quarter_res_pass;

// =============================================================================
// PRECOMPUTED PROPERTIES (Set by NishitaSky script)
// =============================================================================
uniform float precomputed_sun_visible = 1.0;
uniform float precomputed_sun_enabled = 1.0;
uniform mat3 precomputed_moon_dir = mat3(0.0);
uniform vec3 precomputed_sun_dir = vec3(1.0, 0.0, 0.0);
uniform vec3 precomputed_sun_color : source_color = vec3(1.0);
uniform vec3 precomputed_Atmosphere_sun : source_color = vec3(0.0);
uniform vec3 precomputed_Atmosphere_ambient : source_color = vec3(0.0);
uniform vec3 precomputed_Atmosphere_ground : source_color = vec3(0.0);
uniform float precomputed_sun_size : hint_range(0, 90.0) = 9.250245035569947e-03;
uniform float precomputed_sun_energy = 1.0;
uniform float precomputed_background_intensity = 1.0;

// =============================================================================
// ATMOSPHERE SCATTERING PARAMETERS
// =============================================================================
uniform vec3 rayleigh_color = vec3(0.1148036253776435, 0.4078549848942598, 1.0);
uniform float rayleigh : hint_range(0, 64) = 1.0;
uniform vec3 mie_color = vec3(1.0, 1.0, 1.0);
uniform float mie : hint_range(0, 64) = 1.0;
uniform float mie_eccentricity : hint_range(-1, 0.99999) = 0.76;
uniform float turbidity : hint_range(0, 1000) = 1.0;

// =============================================================================
// SAMPLING PARAMETERS
// =============================================================================
uniform int atmosphere_samples_max = 32;
uniform int atmosphere_samples_min = 12;
uniform float atmosphere_samples_horizon_bias = 0.5;
uniform int atmosphere_sun_samples = 32;
uniform int atmosphere_light_samples = 8;

// =============================================================================
// BRIGHTNESS CONTROLS
// =============================================================================
uniform float intensity = 10.0;
uniform float sun_brightness = 10000.0;
uniform float ground_brightness = 1.0;
uniform float night_sky_brightness = 1000.0;

// =============================================================================
// CELESTIAL BODIES
// =============================================================================
uniform vec3 moon_eclipse_color : source_color = vec3(1.0, 0.1, 0.0);
uniform float moon_size_mult = 1.0;
uniform sampler2D moon_texture : source_color, hint_default_black;

// =============================================================================
// GROUND AND ENVIRONMENT
// =============================================================================
uniform vec3 ground_color : source_color = vec3(0.1, 0.07, 0.034);
uniform sampler2D ground_texture : source_color, hint_default_white;
uniform vec3 ground_rotation = vec3(0.0);
uniform sampler2D night_sky : source_color, hint_default_black;

// =============================================================================
// WORLD PARAMETERS
// =============================================================================
uniform float Height = 1000.0;
uniform float earthRadius = 6360e3;
uniform float moonRadius = 1738e3;
uniform float moonDistance = 384400e3;
uniform float atmosphereRadius = 6420e3;
uniform float rayleighScaleHeight = 7994.0;
uniform float mieScaleHeight = 1200.0;

// =============================================================================
// CLOUD PARAMETERS
// =============================================================================
uniform bool clouds = true;
uniform int cloud_samples_horizon = 54;
uniform int cloud_samples_sky = 96;
uniform float cloud_density : hint_range(0.01, 0.5) = 0.097;
uniform float cloud_coverage : hint_range(0.0, 1.0) = 0.25;
uniform float cloud_time_scale : hint_range(0.0, 20.0) = 1.0;
uniform float cloud_time_offset = 0.0;
uniform float cloud_bottom = 1500.0;
uniform float cloud_top = 4000.0;
uniform float cloud_brightness = 1.0;
uniform float cloud_ambient_brightness = 0.5;

uniform sampler3D worlnoise : filter_linear_mipmap, repeat_enable;
uniform sampler3D perlworlnoise : filter_linear_mipmap, repeat_enable;
uniform sampler2D weathermap : filter_linear_mipmap, repeat_enable;

// =============================================================================
// CONSTANTS
// =============================================================================
const float BETA_R_SCALE = 33.1e-6;
const float BETA_M_SCALE = 21e-6;
const float WEATHER_SCALE = 0.00006;

const vec3 RANDOM_VECTORS[6] = {
	vec3(0.38051305, 0.92453449, -0.02111345),
	vec3(-0.50625799, -0.03590792, -0.86163418),
	vec3(-0.32509218, -0.94557439, 0.01428793),
	vec3(0.09026238, -0.27376545, 0.95755165),
	vec3(0.28128598, 0.42443639, -0.86065785),
	vec3(-0.16852403, 0.14748697, 0.97460106)
};

const vec4 STRATUS_GRADIENT = vec4(0.02, 0.05, 0.09, 0.11);
const vec4 STRATOCUMULUS_GRADIENT = vec4(0.02, 0.2, 0.48, 0.625);
const vec4 CUMULUS_GRADIENT = vec4(0.01, 0.0625, 0.78, 1.0);

// =============================================================================
// UTILITY FUNCTIONS
// =============================================================================

// Hash function for noise generation
float hash(vec3 p) {
	p = fract(p * 0.3183099 + 0.1);
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Remap values from one range to another
float remap(float value, float old_min, float old_max, float new_min, float new_max) {
	return new_min + (((value - old_min) / (old_max - old_min)) * (new_max - new_min));
}

// Solve quadratic equation for sphere intersection
vec3 solve_quadratic(vec3 origin, vec3 dir, float radius) {
	float b = 2.0 * dot(dir, origin);
	float c = dot(origin, origin) - radius * radius;
	float discriminant = b * b - 4.0 * c;
	float sqrt_d = sqrt(discriminant);
	float x1 = (-b + sqrt_d) * 0.5;
	float x2 = (-b - sqrt_d) * 0.5;
	return vec3(x1, x2, discriminant);
}

// Henyey-Greenstein phase function for scattering
float henyey_greenstein(float cos_theta, float g) {
	const float k = 0.0795774715459;
	return k * (1.0 - g * g) / pow(1.0 + g * g - 2.0 * g * cos_theta, 1.5);
}

// Rotate point around axis by angle
vec3 rotate_point(vec3 point, vec3 axis, float angle) {
	float s = sin(angle);
	float c = cos(angle);
	float oc = 1.0 - c;

	return vec3(
		point.x * (oc * axis.x * axis.x + c) + point.y * (oc * axis.x * axis.y - axis.z * s) + point.z * (oc * axis.x * axis.z + axis.y * s),
		point.x * (oc * axis.x * axis.y + axis.z * s) + point.y * (oc * axis.y * axis.y + c) + point.z * (oc * axis.y * axis.z - axis.x * s),
		point.x * (oc * axis.x * axis.z - axis.y * s) + point.y * (oc * axis.y * axis.z + axis.x * s) + point.z * (oc * axis.z * axis.z + c)
	);
}

// Convert 3D sphere coordinates to UV coordinates
vec2 sphere_to_uv(vec3 point, vec3 rotation) {
	point = rotate_point(rotate_point(point, vec3(1.0, 0.0, 0.0), rotation.x), vec3(0.0, 1.0, 0.0), rotation.y);
	float theta = atan(point.x, point.y);
	float phi = acos(-point.z / length(point));
	return vec2((theta - (rotation.z - 1.0)) * 0.5, phi) / PI;
}

// Map sphere normal from 2D coordinates
vec3 map_sphere_normal(vec3 x, vec3 y, vec3 z, vec2 point) {
	return x * point.x + y * point.y + z * sqrt(1.0 - point.x * point.x - point.y * point.y);
}

// =============================================================================
// CLOUD FUNCTIONS
// =============================================================================

float get_height_fraction(float position) {
	return clamp((position - cloud_bottom - earthRadius) / (cloud_top - cloud_bottom), 0.0, 1.0);
}

vec4 mix_cloud_gradients(float cloud_type) {
	float stratus = 1.0 - clamp(cloud_type * 2.0, 0.0, 1.0);
	float stratocumulus = 1.0 - abs(cloud_type - 0.5) * 2.0;
	float cumulus = clamp(cloud_type - 0.5, 0.0, 1.0) * 2.0;
	return STRATUS_GRADIENT * stratus + STRATOCUMULUS_GRADIENT * stratocumulus + CUMULUS_GRADIENT * cumulus;
}

float density_height_gradient(float height_frac, float cloud_type) {
	vec4 gradient = mix_cloud_gradients(cloud_type);
	return smoothstep(gradient.x, gradient.y, height_frac) - smoothstep(gradient.z, gradient.w, height_frac);
}

float sample_cloud_density(vec3 position, vec3 weather, float mip_level) {
	float time = mod(TIME, 100.0);
	vec3 p = position;
	p.x += time * cloud_time_scale + cloud_time_offset;
	
	float height_fraction = get_height_fraction(length(p));
	vec4 noise = textureLod(perlworlnoise, p.xyz * 0.00008, mip_level - 2.0);
	float fbm = noise.g * 0.625 + noise.b * 0.25 + noise.a * 0.125;
	float gradient = density_height_gradient(height_fraction, weather.r);
	float base_cloud = remap(noise.r, -(1.0 - fbm), 1.0, 0.0, 1.0);
	float weather_coverage = cloud_coverage * weather.b;
	base_cloud = remap(base_cloud * gradient, 1.0 - weather_coverage, 1.0, 0.0, 1.0);
	base_cloud *= weather_coverage;
	
	p.xy -= time * 4.0 * cloud_time_scale + cloud_time_offset;
	vec3 high_noise = textureLod(worlnoise, p * 0.001, mip_level).rgb;
	float high_fbm = high_noise.r * 0.625 + high_noise.g * 0.25 + high_noise.b * 0.125;
	high_fbm = mix(high_fbm, 1.0 - high_fbm, clamp(height_fraction * 4.0, 0.0, 1.0));
	base_cloud = remap(base_cloud, high_fbm * 0.4 * height_fraction, 1.0, 0.0, 1.0);
	
	return pow(clamp(base_cloud, 0.0, 1.0), (1.0 - height_fraction) * 0.8 + 0.5);
}

// =============================================================================
// ATMOSPHERE FUNCTIONS
// =============================================================================

vec3[5] calculate_atmosphere(vec3 direction, vec3 position) {
	vec3 sun_direction = precomputed_sun_dir;
	vec3 camera_pos = vec3(0, earthRadius + Height + max(0.0, position.y), 0);
	vec3 start = camera_pos;
	vec3 end = vec3(0.0);

	// Find atmosphere intersection
	vec3 atmosphere_intersection = solve_quadratic(camera_pos, direction, atmosphereRadius);
	if (atmosphere_intersection.x > atmosphere_intersection.y && atmosphere_intersection.x > 0.0) {
		end = camera_pos + direction * atmosphere_intersection.x;
		if (atmosphere_intersection.y > 0.0) {
			start = camera_pos + direction * atmosphere_intersection.y;
		}
	} else {
		return {vec3(0.0), vec3(1.0), vec3(0.0), vec3(1.0), end};
	}

	// Check ground intersection
	float ground_hit = 0.0;
	vec3 ground_intersection = solve_quadratic(camera_pos, direction, earthRadius);
	if (ground_intersection.x > 0.0 && ground_intersection.y > 0.0) {
		end = camera_pos + direction * ground_intersection.y;
		ground_hit = 1.0;
	}

	// Calculate scattering
	vec3 sum_rayleigh = vec3(0.0);
	vec3 sum_mie = vec3(0.0);
	float mu = dot(direction, sun_direction);
	float phase_rayleigh = (3.0 / (16.0 * PI)) * (1.0 + mu * mu);
	float phase_mie = (3.0 / (8.0 * PI)) * ((1.0 - mie_eccentricity * mie_eccentricity) * (1.0 + mu * mu) / 
		((2.0 + mie_eccentricity * mie_eccentricity) * pow(1.0 + mie_eccentricity * mie_eccentricity - 2.0 * mie_eccentricity * mu, 1.5)));

	float segment_length = distance(start, end);
	float horizon = sin(acos(earthRadius / (earthRadius + Height)));

	// Adaptive sampling
	float weighted_samples = ceil(clamp(
		max(clamp(1.0 - pow(abs(direction.y + horizon), atmosphere_samples_horizon_bias), 0.0, 1.0) * float(atmosphere_samples_max),
			pow(max(mu, 0.0), 3.0) * float(atmosphere_sun_samples)),
		float(atmosphere_samples_min), 
		float(atmosphere_samples_max)));

	segment_length /= weighted_samples;

	float optical_depth_rayleigh = 0.0;
	float optical_depth_mie = 0.0;
	vec3 atmosphere_attenuation = vec3(0.0);
	vec3 beta_rayleigh = rayleigh_color * BETA_R_SCALE * rayleigh;
	vec3 beta_mie = mie_color * BETA_M_SCALE * mie;

	// Sample atmosphere
	for (float i = 0.5; i < weighted_samples + 0.5; i++) {
		vec3 sample_pos = start + direction * segment_length * i;
		float sample_height = length(sample_pos) - earthRadius;

		float height_rayleigh = exp(-sample_height / (rayleighScaleHeight * turbidity)) * segment_length;
		float height_mie = exp(-sample_height / (mieScaleHeight * turbidity)) * segment_length;

		optical_depth_rayleigh += height_rayleigh;
		optical_depth_mie += height_mie;

		if (precomputed_sun_enabled < 0.5) continue;

		// Light sampling
		vec3 sun_intersection = solve_quadratic(sample_pos, sun_direction, atmosphereRadius);
		vec3 sun_ground_intersection = solve_quadratic(sample_pos, sun_direction, earthRadius);

		if ((sun_ground_intersection.x > 0.0 && sun_ground_intersection.y > 0.0) || sun_intersection.z < 0.0) continue;

		float light_segment_length = max(sun_intersection.x, sun_intersection.y) / float(atmosphere_light_samples);
		float optical_depth_light_rayleigh = 0.0;
		float optical_depth_light_mie = 0.0;

		int valid_samples = 0;
		for (float j = 0.5; j < float(atmosphere_light_samples) + 0.5; j++) {
			float light_sample_height = length(sample_pos + sun_direction * light_segment_length * j) - earthRadius;
			if (light_sample_height < 0.0) break;
			
			optical_depth_light_rayleigh += exp(-light_sample_height / (rayleighScaleHeight * turbidity));
			optical_depth_light_mie += exp(-light_sample_height / (mieScaleHeight * turbidity));
			valid_samples++;
		}

		if (valid_samples == atmosphere_light_samples) {
			optical_depth_light_rayleigh *= light_segment_length;
			optical_depth_light_mie *= light_segment_length;
			vec3 tau = beta_rayleigh * (optical_depth_rayleigh + optical_depth_light_rayleigh) + 
					   beta_mie * 1.1 * (optical_depth_mie + optical_depth_light_mie);
			vec3 attenuation = exp(-tau);

			atmosphere_attenuation += attenuation;
			sum_rayleigh += height_rayleigh * attenuation;
			sum_mie += height_mie * attenuation;
		}
	}

	vec3 sky_color = sum_rayleigh * phase_rayleigh * beta_rayleigh + sum_mie * phase_mie * beta_mie;
	return {sky_color, atmosphere_attenuation, vec3(ground_hit), 
			exp(-(optical_depth_rayleigh * beta_rayleigh + optical_depth_mie * beta_mie)), end};
}

vec4 march_clouds(vec3 start_pos, vec3 end_pos, vec3 ray_dir, int sample_count, float sun_visibility, float density_multiplier) {
	float transmittance = 1.0;
	float alpha = 0.0;
	vec3 light_dir = precomputed_sun_dir;
	float step_size = length(ray_dir);
	ray_dir = ray_dir / step_size;
	vec3 current_pos = start_pos + hash(start_pos * 10.0) * step_size;
	float total_distance = cloud_top - cloud_bottom;
	float light_step_size = total_distance / 36.0;
	vec3 accumulated_light = vec3(0.0);
	float cos_theta = dot(light_dir, ray_dir);
	
	// Multi-phase function for realistic scattering
	float phase = max(max(henyey_greenstein(cos_theta, 0.6),
						  henyey_greenstein(cos_theta, 0.4 - 1.4 * light_dir.y)),
					  henyey_greenstein(cos_theta, -0.2));
	
	vec3 atmosphere_sun = precomputed_Atmosphere_sun * step_size * cloud_brightness * precomputed_sun_energy * phase;
	vec3 atmosphere_ambient = precomputed_Atmosphere_ambient * cloud_ambient_brightness * intensity;
	vec3 atmosphere_ground = precomputed_Atmosphere_ground * ground_color.xyz * ground_brightness * precomputed_sun_energy * intensity;

	float time = mod(TIME, 10000.0) * 0.0003 * cloud_time_scale + 0.005 * cloud_time_offset;
	vec2 weather_offset = vec2(time * 0.9, time);

	for (int i = 0; i < sample_count; i++) {
		current_pos += ray_dir * step_size;
		vec3 weather_sample = texture(weathermap, current_pos.xz * WEATHER_SCALE + 0.5 + weather_offset).xyz;
		float height_fraction = get_height_fraction(length(current_pos));

		float density = sample_cloud_density(current_pos, weather_sample, 0.0);
		float density_term = exp(-density_multiplier * density * step_size);
		transmittance *= density_term;
		
		if (density > 0.0) {
			vec3 light_pos = current_pos;
			float light_transmittance = 1.0;
			float cumulative_density = 0.0;

			// Light sampling
			for (float j = 0.0; j < 6.0 * sun_visibility; j++) {
				light_pos += (light_dir + RANDOM_VECTORS[int(j)] * j) * light_step_size;
				vec3 light_weather = texture(weathermap, light_pos.xz * WEATHER_SCALE + 0.5 + weather_offset).xyz;
				cumulative_density += sample_cloud_density(light_pos, light_weather, j);
			}

			// Distant sample
			light_pos = current_pos + light_dir * 18.0 * light_step_size;
			float light_height_fraction = get_height_fraction(length(light_pos));
			vec3 light_weather = texture(weathermap, light_pos.xz * WEATHER_SCALE + 0.5).xyz;
			cumulative_density += pow(sample_cloud_density(light_pos, light_weather, 5.0), 
									  (1.0 - light_height_fraction) * 0.8 + 0.5);

			// Beer's law for light attenuation
			float beer_term = exp(-density_multiplier * cumulative_density * light_step_size);
			float beer_term_2 = exp(-density_multiplier * cumulative_density * light_step_size * 0.25) * 0.7;
			float total_beer = max(beer_term, beer_term_2);

			vec3 ambient = mix(atmosphere_ground, atmosphere_ambient, smoothstep(0.0, 1.0, height_fraction)) * beer_term;
			alpha += (1.0 - density_term) * (1.0 - alpha);
			accumulated_light += ((ambient + total_beer * atmosphere_sun) * alpha) * transmittance * density;
		}
	}
	
	return vec4(accumulated_light * cloud_brightness, clamp(alpha, 0.0, 1.0));
}

vec4[4] sample_sky_color(vec3 direction, vec3 position, float visibility) {
	vec3[5] sky_data = calculate_atmosphere(direction, position);
	
	// Ground UV calculation
	vec3 ground_intersection = solve_quadratic(vec3(0, earthRadius + Height + max(0.0, POSITION.y), 0), direction, earthRadius);
	vec3 ground_point = sky_data[4] / earthRadius * 2.0;
	vec2 ground_uv = sphere_to_uv(
		rotate_point(rotate_point(ground_point, vec3(0.0, 0.0, 1.0), position.x * PI / (earthRadius + position.y + Height)),
					 vec3(1.0, 0.0, 0.0), position.z * PI / (earthRadius + position.y + Height)),
		ground_rotation * 0.5);

	// Sun disk
	vec3 sun_color = (vec3(1.0) - exp(-sky_data[1])) *
					 max(max(dot(direction, precomputed_sun_dir), 0.0) - cos(precomputed_sun_size), 0.0) *
					 sun_brightness * (1.0 - sky_data[2].r) * visibility * precomputed_sun_energy;

	// Ground color
	vec3 ground_color_final = (vec3(1.0) - exp(-sky_data[3])) * ground_color *
							  texture(ground_texture, ground_uv).xyz * sky_data[2].r * ground_brightness;
	ground_color_final *= clamp(dot(ground_point, precomputed_sun_dir), 0.0, 1.0);
	
	vec3 final_color = (ground_color_final + sky_data[0].xyz) * precomputed_sun_energy;
	
	return {vec4(final_color * precomputed_sun_color, sky_data[2].r),
			vec4(sky_data[3] * (1.0 - sky_data[2].r), 1.0), 
			vec4(sun_color, 1.0),
			vec4(ground_point, 0.0)};
}

void sky() {
	vec3 view_direction = EYEDIR;
	bool is_full_res = !(AT_HALF_RES_PASS || AT_QUARTER_RES_PASS);
	float horizon = sin(acos(earthRadius / (earthRadius + max(POSITION.y, 0.0) + Height)));
	float horizon_distance = view_direction.y + horizon;

	// Sun visibility calculation
	float sun_visibility = precomputed_sun_visible * max(sign(precomputed_sun_dir.y + 
		sin(acos(earthRadius / (earthRadius + cloud_top)) + precomputed_sun_size)), 0.0);

	vec4 cloud_color = vec4(0.0);
	float cloud_star_fade = 1.0;
	float cloud_fade = 1.0;

	// Cloud rendering
	if (clouds) {
		if ((AT_CUBEMAP_PASS && AT_QUARTER_RES_PASS) || (!AT_CUBEMAP_PASS && AT_HALF_RES_PASS)) {
			float density_multiplier = cloud_density * pow(1.0 - clamp((POSITION.y - cloud_bottom) / (cloud_top - cloud_bottom), 0.0, 1.0), 2.0);
			
			if (horizon_distance > 0.0 && density_multiplier > 0.0) {
				vec3 camera_position = vec3(POSITION.x, min(POSITION.y, cloud_bottom) + earthRadius, POSITION.z);
				vec3 cloud_top_intersection = solve_quadratic(camera_position, view_direction, cloud_top + earthRadius);
				vec3 cloud_bottom_intersection = solve_quadratic(camera_position, view_direction, cloud_bottom + earthRadius);
				
				float cloud_start_distance = cloud_bottom_intersection.x;
				float cloud_end_distance = cloud_top_intersection.x;
				vec3 cloud_start = camera_position + view_direction * cloud_start_distance;
				vec3 cloud_end = camera_position + view_direction * cloud_end_distance;
				
				float shell_distance = cloud_end_distance - cloud_start_distance;
				float sample_count = ceil(mix(
					float(cloud_samples_horizon) * (1.0 - 0.25 * (1.0 - sun_visibility * (1.0 - cloud_coverage))),
					float(cloud_samples_sky) * (1.0 - 0.25 * (1.0 - sun_visibility)),
					sqrt(clamp(view_direction.y + horizon, 0.0, 1.0))));
				
				vec3 ray_step = view_direction * shell_distance / sample_count;
				cloud_color = march_clouds(cloud_start, cloud_end, ray_step, int(sample_count), precomputed_sun_visible, density_multiplier);
				cloud_color.xyz *= precomputed_sun_visible * precomputed_sun_color;
			}
			COLOR = cloud_color.xyz;
			ALPHA = clamp(cloud_color.w, 0.0, 1.0);
		} else if (is_full_res) {
			cloud_fade = clamp(view_direction.y, 0.0, 1.0);
			cloud_star_fade = clamp(horizon_distance, 0.0, 1.0);
			cloud_color = AT_CUBEMAP_PASS ? QUARTER_RES_COLOR : HALF_RES_COLOR;
		}
	}

	if (is_full_res) {
		// Moon calculations
		float moon_size = (moonRadius / length((moonDistance + earthRadius) * precomputed_moon_dir[2] - 
			vec3(0.0, POSITION.y + earthRadius + Height, 0.0)) * 2.0) * moon_size_mult;
		
		float sun_eclipse_visibility = 1.0 - clamp((dot(EYEDIR, precomputed_moon_dir[2]) - 
			sqrt(1.0 - 0.25 * (moon_size / 45.0))) * 100000.0, 0.0, 1.0);

		vec4[4] background = sample_sky_color(view_direction, POSITION, sun_eclipse_visibility);
		
		// Moon rendering
		vec4 moon_color = vec4(0.0);
		vec2 moon_coords = vec2(dot(EYEDIR, precomputed_moon_dir[1]), dot(EYEDIR, precomputed_moon_dir[0])) / sqrt(moon_size / 45.0);
		
		if (dot(EYEDIR, precomputed_moon_dir[2]) > sqrt(1.0 - 0.25 * (moon_size / 45.0))) {
			vec3 moon_normal = normalize(map_sphere_normal(precomputed_moon_dir[1], precomputed_moon_dir[0], -precomputed_moon_dir[2] * 0.5, moon_coords * 2.0));
			moon_color = texture(moon_texture, vec2(0.5) - moon_coords.yx);
			
			float moon_distance_earth_ratio = 1.0 - atan(earthRadius / (moonDistance + earthRadius)) / TAU;
			float earth_shadow = clamp((moon_distance_earth_ratio - max(dot(-normalize(precomputed_moon_dir[2] * (earthRadius + moonDistance) + 
				vec3(0.0, POSITION.y + earthRadius + Height, 0.0)), normalize(precomputed_sun_dir)), 0.0)) / moon_distance_earth_ratio * 200.0, 0.0, 1.0);
			
			moon_color.xyz *= precomputed_background_intensity * precomputed_sun_energy * 
				clamp(dot(moon_normal, precomputed_sun_dir), 0.0, 1.0) * earth_shadow * mix(moon_eclipse_color, vec3(1.0), earth_shadow);
		}

		// Stars and night sky
		vec3 star_color = (texture(night_sky, sphere_to_uv(EYEDIR, 
			vec3(PI * 0.5 + POSITION.z * PI / (earthRadius + POSITION.y + Height),
				 -POSITION.x * PI / (earthRadius + POSITION.y + Height),
				 -PI * 0.5) + ground_rotation * 0.5)).xyz * night_sky_brightness * (1.0 - moon_color.w) + 
			moon_color.xyz * moon_color.w) * background[1].xyz / precomputed_background_intensity;

		cloud_color.a = clamp(cloud_color.a * precomputed_background_intensity / sun_brightness, 0.0, 1.0);
		float cloud_passthrough = (1.0 - cloud_color.a) * cloud_star_fade;

		// Eclipse calculations
		float sun_passthrough = 1.0;
		if (moon_size > 0.0) {
			float moon_attenuation_range = sin(radians(moon_size)) * 0.5;
			sun_passthrough = pow(clamp(1.0 - clamp(min(dot(precomputed_moon_dir[2], precomputed_sun_dir), 1.0) - 
				(1.0 - moon_attenuation_range), 0.0, 1.0) / moon_attenuation_range, 0.0, 1.0), 2.0);
		}

		// Final color composition
		vec3 sky_color = background[0].xyz * intensity * 
			max(precomputed_sun_dir.y, clamp((POSITION.y + Height) / (atmosphereRadius - earthRadius), 0.0, 1.0)) * 
			precomputed_sun_enabled * sun_passthrough;
		
		sky_color = mix(sky_color, vec3(sky_color.x + sky_color.y + sky_color.z) * 0.0033333333, 
			pow(clamp((cloud_coverage - 0.25) / 0.75, 0.0, 1.0), 0.5));
		
		cloud_color.xyz *= sun_passthrough;
		vec3 sun_color = background[2].xyz * intensity * precomputed_sun_enabled;

		COLOR = sky_color + 
				sun_color * (1.0 - cloud_color.w) + 
				cloud_color.xyz * cloud_star_fade * mix(cloud_star_fade, 1.0, clamp(precomputed_sun_dir.y, 0.0, 1.0)) + 
				star_color * (1.0 - cloud_color.w);
	}
}
