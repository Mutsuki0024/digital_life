# Digital Life – Unity ML-Agents Project

## Project Overview
This is a small digital life simulation environment built with **Unity + ML-Agents**.  
The agent can **explore, search for food, avoid traps and walls**, and survive under limited health points and hunger constraints.  
The goal is to train an agent with reinforcement learning to acquire basic survival and exploration abilities.  

---

## Features
- **Agent Behavior**
  - Continuous action space (steering + throttle)  
  - Health system, hunger system, and death condition  
  - Small penalty when colliding with walls  
  - Action consistency penalty: discourages sudden changes in movement to encourage smoother trajectories  
- **Sensors**
  - Forward long-range ray sensor (for detecting food, traps, and walls)  
  - Surrounding near-range ray sensor (to avoid blind spots)  
- **Rewards and Penalties**
  - Positive reward for eating food  
  - Penalty and damage for stepping into traps  
  - Shaping reward for forward movement  
  - Penalties for hunger, death, or wall contact  
  - Optional penalty for abrupt changes in actions  
- **Environment Management**
  - Food and traps are managed by an **object pool** for efficient spawning and recycling  
  - At the beginning of each episode, food and traps are reset to **random positions**  
- **Configurable Parameters**
  - Reward values, agent speed, number of food/traps, episode length, etc.  
  - All parameters can be modified in `Assets/Resources/configs/config`  

---

## Demo
![Training Demo](Assets/demo.gif)

---

## How to Use
### 1. Run the Pre-trained Model
A trained model (`250k_steps.onnx`) has already been included in the project.  
Simply open the Unity project and **compile/run the scene** — the agent will immediately demonstrate its learned behavior.

### 2. Train from Scratch
If you want to retrain the agent:  
```bash
mlagents-learn config/train.yaml --run-id=digital_life --env=build/digital_life.exe --force
```
After training, an MyAgent.onnx model will be exported, which can be attached to the Behavior Parameters component in Unity for inference.


## Skills Learned by the Agent
- Actively searching for food to increase survival time.
- Avoiding traps and walls.
- Escaping from corners and continuing exploration.
- Learning smoother movement patterns due to the action consistency penalty.
- Forming a cycle of exploration → foraging → survival under the hunger mechanism.

## Future work
- Add predator-prey dynamics.
- Introduce more complex map structures.
- Multi-agent cooperation and competition.


