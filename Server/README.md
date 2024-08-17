
# Snake Server - PS9
This README offers a thorough rundown of the features, design choices, and present state of the server for our version of the traditional snake game. Our main objective was to build a feature-rich, reliable server that would allow for a dynamic snake game experience.


## Design Decisions

The key components of our server architecture are scalability and flexibility. Important setups and game parameters are consolidated in the setting.xml file included in the Server project. This design decision makes it simple to adjust and modify the game's parameters.

## Features
- **WrapAround Mode**: This feature is toggleable in setting.xml. It permits the snake to leave the game world from one edge and re-enter from the other when set to true. On the other hand, if you set it to false, the snake will perish when it reaches the bounds of the world.

- **ShrinkSnake Mode**: Another special feature in setting.xml is shrinkSnake. In this challenging mode, each turn the snake makes will shorten its length. Excessive turning can lead to the snake's DEATH, adding an extra layer of strategy and difficulty to the game.
## What Works
- **Core Gameplay**: Every fundamental component of a snake game is effectively used. This covers fundamental mobility, how one eats, growth mechanisms, and avoiding crashes.
## Known Issues
- **Glitch in Rendering**: We occasionally encounter a rendering glitch where the screen appears to teleport erratically, especially noticeable when the snake dies or when WrapAround mode is active. Although rare, it disrupts the gameplay experience.
- **Food in Wall**: We occasionally encounter a rendering glitch where the foods appears to spawn in wall. Though we tried to fix it, but sometimes the problem still occurs
## Future fix
- the reason is related to the way boundary conditions and rendering are handled, and we have carried out early research to identify the source.
- Future releases will concentrate on optimizing the logic managing boundary transitions and screen rendering.
## Development and Contributions
- **Khanh Tran**: Focused on server-client handshake, MVC architecture, and general server logic. His role was instrumental in establishing a stable and responsive game server.
- **Orion Harmon**: Main navigator for developing hitbox logic. The implementation of efficient collision detection systems was made by Orion's.



