# Plan

- Raytracing um ein Schwarzes Loch
- Calc metric Tensor
- Calc Cristophels
- Integrate Geodesic Equation numerically
- Check for hit
- Übertragung auf GPU
- Render rückwärtslaufen lassen

# Change of Plans

- Simulate Light one after another
- Simulate Redshift

# Devlog

- 13.1.2026 Calc Metric Tensor
- 15.1.2026 Dependencies to Calculate Metric Tensor
- 16.1.2026 Function to calculate derivatives of Metric Tensor
- 18.1.2026 Runge Kutta RK4 Integration finished
- 20.1.2026 Checked for Constraints and started debugging
- Be sick for a week 😔
<!--- sry for using Emojis, im not ChatGPT frfr--->
- 28.1.2026 Made function to construct Camera Tetrad and tested it... didn't work 😔 again
- 29.1.2026 Made Heatmap Visualization of H field Strength. Inside the ring singularity r ≈ 0 -> circle singularity (Note 1)
- 1.2 Started Camera Debugging (If you include "making it worse" in the definition of debugging)
- 14-16.3 Worked on drawing picture to screen an simulate collisions with different obejcts like accretion disk, black hole or Sphere of "I don't care to simulate this any further"
- 18.3 Fixed Bug with Camera when Aspect ratio != 1 and fixed holes in Black hole (took 3 days to fix this bug)
(small brake because of holidays and Antikenfahrt)
- 24-26.4 translated to hlsl
- 27.4 made better version of Accretiondisk
- 28.4 fixed Accretiondisk and added Background image
- 19.4 debugged/finished Accretiondisk
- 30.4, 1.5 Implemented Redshift
- 9.5 Added Photo Export
- 15.5 Added Video capabilities
- 16.5 "Added" Live Rendering

## ToDo

- check for constraints in geodesic integration [x]
- check for constraints in all dimensions [ ]
- create local tedrad [x]
- account for colissions [x]
- render image [x]
- account for redshift [x]
- make steps inverse proportional to H so that stronger gravity = smaller step size[ ]
- fix asymetries [x]
- fix escaping [x]

## Debugg Steps:

- metric tensor [x]
- christoffel symbols [x]
- del metric tensor [x]
- RK4 [x]
- Visualize Light Stepping [x]

## Notes

### Note 1: Heatmap

M = 8, a = 4, z = 0, y = 0, x is the variable. 
At x = 0: r = 0, H = NaN 
At 0<x<4: r = 0/very small/NaN, H = very big/ NaN 
At x = 4: r = 0; H = NaN At x > 4: r increases like expected, H rapidly droppes of (like expected)

<br />
<br />
<br />
<!--- brrrrrrrrrr --->

## Interesting / Funny Pictures / Problems encountered while debugging (that will take up way to much space because i such at md)
### Struggles with the GPU:
#### Longhole  <br />
<img width="880" height="493" alt="Screenshot 2026-01-29 110921" src="https://github.com/user-attachments/assets/4f3de198-9565-43c7-92ad-42d482a59599" />  <br />
#### Diagonalhole  <br />
<img width="354" height="372" alt="Screenshot 2026-01-29 104931" src="https://github.com/user-attachments/assets/ff1046a8-a820-466f-8ebc-7a258f0335ab" />  <br />
#### Many Worlds? Na, Many Holes!  <br />
<img width="735" height="739" alt="Screenshot 2026-01-29 161322" src="https://github.com/user-attachments/assets/51b0aff4-8950-4f40-aa4d-0f9dbfae8afb" />  <br />

## Possible next steps:

1. Accelerate with GPU
2. make two regions with different metrics to simulate two black holes and multi gravitational lensing
2. make crude fluid-simulation and newtonian gravity-sim to simulate captured star, Goal: simulate formation of accretion disk

### 1. Accelerate with GPU
- Transfer entire Runge-Kutta to GPU
(gug idea)
- try to crash the Laptop less than 10 times
(Mission failed miserably)
#### Expected Time: < 1 week
(fr fr, past Louis)

### 2. Seperate region simulation
- easy way to incorporate multiple heavy obejects
- Goal: simulate gravitational telescope consiting of multiple black holes in formation
#### Expected Time: < 1 week
(Prob same same)

### 3. Gravitational Fluid Simulation
- Make a Fluid Simulation (not sure which type) combine it with a newtonian gravity simulation to simulate a star
- let the star get "eaten" by the black hole
- Goal: simulate fomation of accretion disk
#### Expected Time: >= 1 Month
(Don't even wanna get started)



