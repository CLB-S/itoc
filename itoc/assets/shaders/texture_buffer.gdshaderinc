ivec2 get_texture_index(int index, int width) {
    return ivec2(index % width, index / width);
}

uint unpack_uint(usampler2D buffer, int bufferWidth, int index) {
    // Unpack a single 32-bit unsigned integer from the buffer at the specified index.
    return texelFetch(buffer, get_texture_index(index, bufferWidth), 0).r;
}

uint unpack_uint(usampler2D buffer, int bufferWidth, uint bitsPerValue, int valueIndex) {
    // bitsPerValue shound be less than 32

    int texelIndex = valueIndex * int(bitsPerValue / 32u);
    uint bitOffset = uint(valueIndex) * bitsPerValue % 32u;
    uint texel = texelFetch(buffer, get_texture_index(texelIndex, bufferWidth), 0).r;

    if (bitsPerValue + bitOffset > 32u) {
        // If the value spans across two texels, we need to fetch the next texel
        uint nextTexel = texelFetch(buffer, get_texture_index(texelIndex + 1, bufferWidth), 0).r;
        return ((texel >> bitOffset) | (nextTexel << (32u - bitOffset))) & ((1u << bitsPerValue) - 1u);
    }

    return (texel >> bitOffset) & ((1u << bitsPerValue) - 1u);
}

uvec2 unpack_ulong(usampler2D buffer, int bufferWidth, int index) {
    // Unpack a single 64-bit unsigned integer from the buffer at the specified index.
    int texelIndex = index * 2;
    uint low = texelFetch(buffer, get_texture_index(texelIndex, bufferWidth), 0).r;
    uint high = texelFetch(buffer, get_texture_index(texelIndex + 1, bufferWidth), 0).r;
    return uvec2(low, high);
}


// WARNING: Not tested yet.
uvec2 unpack_ulong(usampler2D buffer, int bufferWidth, uint bitsPerValue, int valueIndex) {
    // 32 < bitsPerValue < 64
    
    int texelIndex = valueIndex * int(bitsPerValue / 32u);
    uint bitOffset = uint(valueIndex) * bitsPerValue % 32u;
    uint texel = texelFetch(buffer, get_texture_index(texelIndex, bufferWidth), 0).r;
    uint nextTexel = texelFetch(buffer, get_texture_index(texelIndex + 1, bufferWidth), 0).r;

    uint low = ((texel >> bitOffset) | (nextTexel << (32u - bitOffset)));

    if (bitsPerValue + bitOffset > 64u) {
        // If the value spans across three texels, we need to fetch the next texel
        uint thirdTexel = texelFetch(buffer, get_texture_index(texelIndex + 2, bufferWidth), 0).r;

        uint high = ((nextTexel >> bitOffset) | (thirdTexel << (32u - bitOffset))) & ((1u << (bitsPerValue - 32u)) - 1u);
        return uvec2(low, high);
    }

    return uvec2(low, (texel >> bitOffset) & ((1u << (bitsPerValue - 32u)) - 1u));
}