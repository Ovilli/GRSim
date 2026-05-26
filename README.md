# GRSim

> Visualising deviations from the Minkowski metric in Kerr spacetime.

<img width="1920" height="1080" alt="Render 1" src="https://github.com/user-attachments/assets/0a9f0bbc-5366-4c69-b80a-f59cb4e2bb6c" />

<img width="3840" height="2160" alt="Render 2" src="https://github.com/user-attachments/assets/7594e694-5aa9-4c6d-ad3b-e07a1dbf75df" />

## Videos

[![](https://img.youtube.com/vi/kiZFMop2YXI/0.jpg)](https://youtu.be/kiZFMop2YXI)
[![](https://img.youtube.com/vi/J-dq-xoaSqQ/0.jpg)](https://youtu.be/J-dq-xoaSqQ)
[![](https://img.youtube.com/vi/kqanx4Q8beg/0.jpg)](https://youtu.be/kqanx4Q8beg)
[![](https://img.youtube.com/vi/0EBSjOWegvo/0.jpg)](https://youtu.be/0EBSjOWegvo)

## Simulation Output

Simulated H — deviation from the Minkowski metric.

| (500x500), M = 80, a = 60, z = 0, Rim = 700 | (500x500), M = 8, a = 4.5, z = 0, Rim = 300 |
| ------------- | ------------- |
| <img width="721" height="736" src="https://github.com/user-attachments/assets/0c52a7d0-55c6-4be6-a808-8b68ca1f0fde" /> | <img width="713" height="728" src="https://github.com/user-attachments/assets/f42524e3-bf64-479a-bad5-7953b826e040" /> |
| Extreme spin — unrealistic, a = 60 is far beyond the Kerr bound | More physically reasonable parameters |

## Colour Key

| Colour | Meaning |
|--------|---------|
| Dark   | Less deviation from flat spacetime |
| Light  | More deviation |
| Red    | Ring singularity |
| Blue   | Ergosphere |
| Purple | Non-singular coordinate point behaving as a singularity under H — use the Kretschmann scalar to confirm it is not a true singularity |

## Parameters

| Parameter | Description |
|-----------|-------------|
| `M`   | Mass |
| `a`   | Spin (Kerr parameter) |
| `z`   | Axial slice |
| `Rim` | Singularity ring radius — higher values make the ring appear larger |
