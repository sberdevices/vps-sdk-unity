# VPS SDK (Unity3D)

This is **Visual Positioning System** SDK for Unity engine. Main features are:
- High-precision global user position localization for your AR apps
- Easy to use public API and prefabs
- Supports Android and iOS target platforms
- Integration in ARFoundation (for ARCore and ARKit)

For more information visit [our page on SmartMarket](https://developers.sber.ru/portal/tools/visual-positioning-system-sdk). If you want access to other VPS locations or want to scan your own proprerty, please contact us at <arvrlab@sberbank.ru>.

## Requirements

- Unity 2021.1+
- ARKit or ARCore supported device

## Installation

- Clone this repository. Requires installed [Git-LFS](https://git-lfs.github.com).
- Add git URL from the Package Maneger UI to your project:
`https://github.com/sberdevices/vps-sdk-unity.git?path=/Assets/`

## Examples

SDK includes an example scene with basic setup and graphics. Load project in Unity Editor and open `Scenes/TestScene`. You can start this scene in Editor or build on your device.

## Usage

### VPS Settings
You can adjust VPS behaviour by changing public fields in `VPSLocalisationService` component:

| Property Name | Description | Default |
| ------ | ------ | ------ |
| **Start On Awake** | Should VPS start on Awake or be activated manually. | true |
| **Use Mock** | Use mock provider when VPS service has started. Allows to test in Editor. | false |
| **Force Mock in Editor** | Always use mock provider in Editor, even if UseMock is false .| true |
| **Use Photo Series** | Use several images for first localization. Recomended for indoor locations. | false |
| **Send Only Features** | Process images with neural network. Mandatory for production-ready apps. | true |
| **Always Force** | Ignore user previous positions. Recomended for outdoor locations. | true |
| **Send GPS** | Send user GPS location. Recomended for outdoor locations. | true |
| **Default URL** | URL to your VPS location server. | ... |
| **Default Building GUID** | Unique ID of your location. | ... |
