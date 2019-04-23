
## LifxNet

See upstream `dotMorten/LifxNet` for nuget package

Based on the official [LIFX protocol docs](https://github.com/LIFX/lifx-protocol-docs)

#### Usage

Note: Be careful with sending too many messages to your bulbs - LIFX recommends a max of 20 messages pr second pr bulb. 
This is especially important when using sliders to change properties of the bulb - make sure you use a throttling
mechanism to avoid issues with your bulbs. See the sample app for one way to handle this.
