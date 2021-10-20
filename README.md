# VPS SDK (Unity3D)

This is **Visual Positioning System** SDK for Unity3D engine. Main features are:
- High-precision global user position localization for your AR apps
- Easy to use public API and prefabs
- Supports Android and iOS target platforms
- Integration in ARFoundation (for ARCore and ARKit)

For more information visit [our page on SmartMarket](https://developers.sber.ru/portal/tools/visual-positioning-system-sdk). If you want access to other VPS locations or want to scan your own proprerty, please contact us at <arvrlab@sberbank.ru>.

## Requirements

- Unity 2021.1+
- ARKit or ARCore supported device

## Installation

Just clone this repository. Requires installed [Git-LFS](https://git-lfs.github.com).

You can also add git URL to the Package Maneger UI in your project dependencies:
```
https://github.com/sberdevices/vps-sdk-unity.git?path=/Assets/
```

## Examples

SDK includes an example scene with basic setup and graphics. Load project in Unity Editor and open `Scenes/TestScene`. You can start this scene in Editor or build on your device.

## Usage

### Testing your app

When you start VPS in Editor, it loads an image from `Mock Provider`. You can change this image by selecting `VPS/MockData/FakeCamera` component in Example scene hierarchy.

You can also enable `Mock Mode` for device builds. Just toggle `Use Mock` property in `VPS/VPSLocalisationService` and rebuild your app.

### VPS Settings

You can adjust VPS behaviour by changing public properties in `VPSLocalisationService` component:

| Property Name | Description | Default |
| ------ | ------ | ------ |
| **Start On Awake** | Should VPS start on Awake or be activated manually. | true |
| **Use Mock** | Use mock provider when VPS service has started. Allows to test VPS in Editor. | false |
| **Force Mock in Editor** | Always use mock provider in Editor, even if UseMock is false .| true |
| **Use Photo Series** | Use several images for first localization. Recomended for indoor locations. | false |
| **Send Only Features** | Process images with neural network. Mandatory for production-ready apps. | true |
| **Always Force** | Ignore user previous positions. Recomended for outdoor locations. | true |
| **Send GPS** | Send user GPS location. Recomended for outdoor locations. | true |
| **Default URL** | URL to your VPS location server. | ... |
| **Default Building GUID** | Unique ID of your location. | ... |

## License 

This project is licensed under [Sber Public License at-nc-sa v.2](LICENSE).

TensorFlow library is licensed under [Apache License 2.0.](https://github.com/tensorflow/tensorflow/blob/master/LICENSE)
