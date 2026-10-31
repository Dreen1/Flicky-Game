# Flick Battle in Unity: Setup Guide

These steps take you from nothing to Flick Battle running on your phone. You'll need a Mac, since Unity doesn't run on an iPad.

## What you need

| Item | Cost | Notes |
|---|---|---|
| Unity Personal (Unity 6 LTS) | Free | Free while revenue is under $200K/yr |
| A Mac with Xcode | Free if you own a Mac | Apple requires a Mac to build iPhone apps |
| Apple Developer account | $99/year | Needed for the App Store. Testing on your own iPhone works with a free Apple ID |
| Google Play Console | $25 one time | Needed to publish on Android |
| Art asset packs (optional) | About $20–150 | Better ships, explosions, backgrounds (see "Graphics" below) |

## Step 1: Install Unity
1. Download **Unity Hub** from unity.com/download
2. In Unity Hub go to **Installs → Install Editor**, pick the latest **Unity 6 LTS**
3. Tick these modules: **iOS Build Support** and **Android Build Support** (plus OpenJDK and Android SDK & NDK Tools under it)
4. Install **Xcode** from the Mac App Store

## Step 2: Create the project
1. Unity Hub → **Projects → New Project** → pick the **Universal 3D** template (it may say URP)
2. Name it `FlickBattle` and click **Create project**
3. Download this repo: on GitHub click the green **Code** button → **Download ZIP**, then unzip it
4. In Unity's **Project** window (bottom of the screen), right-click **Assets** → **Reveal in Finder**
5. From the unzipped download, copy the **Scripts** and **Editor** folders (inside `FlickBattleUnity/Assets`) into that Assets folder in Finder
6. Go back to Unity and wait for it to finish loading. A spinner shows in the bottom-right corner while it works

## Step 3: Two one-time settings
1. **Edit → Project Settings → Player → Other Settings → Active Input Handling** → set it to **Both**. Unity asks to restart: click **Yes**
2. **Window → TextMeshPro → Import TMP Essential Resources** → click **Import**

## Step 4: Build the game with one click
1. In the menu bar at the top of the screen, click **Flick Battle → Build Scene**
2. Click **Build it**
3. When it says "Flick Battle is ready", press the **▶ Play** button at the top of Unity

Click a ship, then quickly drag and let go to flick it. Mouse works for testing in Unity.

The button builds the scene, the ships, explosions, stars, a gas giant, glow effects, sounds, buttons and score text, and sets the game to landscape. You can click it again anytime to rebuild from scratch. It replaces the `Assets/FlickBattle` folder, so don't keep your own files in there.

**If something goes wrong:** open **Window → General → Console**, copy the red error text, and send it to Claude.

## Step 5: Test on your phone
- **iPhone:** **File → Build Profiles → iOS → Switch Platform → Build**. Pick a folder. When it finishes, open the `.xcodeproj` file inside it in Xcode, go to **Signing & Capabilities**, choose your Apple ID as the Team, plug in your iPhone and press ▶ Run. The first time, the iPhone asks you to trust the developer: **Settings → General → VPN & Device Management**
- **Android:** turn on Developer Mode and USB debugging on the phone, plug it in, then **File → Build Profiles → Android → Switch Platform → Build And Run**

---

## Graphics: getting close to R-Type Final 2

The one-click build gives you glowing placeholder ships made of basic shapes, with bloom glow, rim lighting, stars and a planet. R-Type Final 2 was made by a professional studio with custom 3D models, so for that level of detail you add **art packs**:

1. **3D ship models:** search the Unity Asset Store for "sci-fi spaceship pack". Pick a sleek fighter for Blue and a round saucer or organic shape for Red. Open `Assets/FlickBattle/Prefabs/BlueShip`, delete its **Model** child and drop the new ship in its place, with the **nose pointing right**
2. **Explosions:** search "sci-fi explosions". Drag one into each ship prefab's **Explosion Prefab** slot
3. **Background:** a nebula image or skybox, and a gas-giant texture on the **Gas Giant** sphere
4. **Sound:** search "sci-fi SFX" on the Asset Store or freesound.org, then drop clips into the GameManager's Whoosh, Explosion, Miss and Win slots

Ship art can change without touching the code. Phones need some care: keep ship models under about 10k triangles each.
