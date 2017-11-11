# UnityPlanets
Full-scale procedural planet generator in Unity with lots of graphics tricks including:
- Scaled-space approach for rendering full-size planets
- Geometry shader grass, fully integrated into Unity's forward and deferred rendering pipelines
- Custom light shapes (capsule + box), integrated into Unity's deferred rendering pipeline
- Screen-space atmosphere as a post-process
- Custom tree generator with animated leaves

## Planned
------
- VR support
- Place trees on the surface, with billboarding in the distance
- Randomly generated dungeons to place on the surface
- Change light optical depth calculation in Scatterer.cginc to incorporate a directional light's shadow buffer
- Better (physically based) clouds
- Better drag/atmosphere model

## Screenshots
------
### Planet Surface
![1](https://i.imgur.com/UtdnsyH.png "")
![2](https://i.imgur.com/S2tt9vB.png "")
![3](https://i.imgur.com/k198C7x.png "")
![4](https://i.imgur.com/FQZeYqE.png "")
![5](https://i.imgur.com/axUi5wJ.png "")
### Interior Lighting with Capsule Lights
![6](https://i.imgur.com/MG1SvSs.png "")
![7](https://i.imgur.com/ZRZzW6s.png, "")
![8](https://i.imgur.com/v01NC9e.png, "")
![9](https://i.imgur.com/CfUkubK.png, "")
### Tree Generator
![10](https://i.imgur.com/cVmAa8Z.png, "")
![11](https://i.imgur.com/5FebkQm.png, "")
![12](https://i.imgur.com/MTh12qB.png, "")
