
# Unity Game 🎮
A LoL like battle arena game using Unity 6000.0.40f

## In progress...
- [ ] In-Game UI
  - [ ] HUD
    - [ ] Champ stats
      - [ ] Styling
      - [ ] Icons
      - [ ] Bind values   
    - [ ] Mini-map
      - [ ] Add background texture 
    - [ ] FPS counter
    - [ ] In-Game Chat
      - [x] Network Logic
      - [x] Enable/Disable via keyboard input
      - [ ] Automated System messages
        - [ ] Player Connected
        - [ ] Player Disconnected
        - [ ] Game Mode Events (?)
      - [ ] Timestamps
    - [ ] Health/Mana bars
      - [ ] Bind values   
- [ ] VFX
    - [ ] W ability
    - [ ] R ability
    - [ ] Status effects on players

## Gamemodes
- Single Player
- Multiplayer
  - Duel - 1v1
  - ? - 5v5
 
## Multiplayer Architecture  
- Unity's Netcode for Game Objects
- Dedicated server hosted by Unity Game Server Hosting
- Server authoritative
- Client side prediction (server reconciliation WIP)
- Linux x86 server build

## Scenes
- Main Menu
- Arena (single player, broken atm)
- Multiplayer
- Death Scene

## Test scenes
- UI Testing
- Testing

## Planned scenes
- Settings (global)
- Settings (multiplayer)

### Future tasks...
- Environment Design (Duel map)
- Re-do/improve E VFX

## Screenshots

# Duel HUD
![Duel HUD](https://i.imgur.com/PpePVhl.png)
# Create lobby modal
![Create lobby modal](https://i.imgur.com/sBROwR7.png)
# Lobby host view
![Lobby host view](https://i.imgur.com/TMmyq0I.png)
# Lobby list view
![Lobby list view](https://i.imgur.com/2a7Z5M2.png)
# Lobby player view
![Lobby player view](https://i.imgur.com/g8nGJMD.png)

