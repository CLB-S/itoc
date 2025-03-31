# TODOs

> Number of ⭐ means current priority.

## Blocks

- [ ] More blocks and a better block management system.

## Chunks

- [ ] LOD
- [ ] ⭐⭐ Chunk.cs | `SetBlock()`, `GetBlock()`.
- [ ] Empty/full chunks.
- [x] ⭐⭐⭐ Chunk.cs & ChunkMesher.cs | Fix `DirectionalBlock` rendering.

## World

- [ ] ⭐⭐ `SetBlock()`, `GetBlock()`.
- [ ] ⭐ Use `TerrainGenerator` for terrain generation.
- [ ] ⭐⭐ Global chunk/block data storage. The chunk size is 62x62x62, while `ChunkMesher` takes (62+2)^3 voxels including neighboring chunk data as input. See [cgerikj/binary-greedy-meshing](https://github.com/cgerikj/binary-greedy-meshing). Related logic needs implementation.

## Terrain

- [ ] ⭐ Revise `TerrainGenerator`.

## Player

- [x] ⭐⭐ Add the player.
  - [x] ⭐⭐ Movement
  - [ ] ⭐ Place/break blocks
  - [ ] Model
