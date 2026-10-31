# Flick Battle in Unity: Setup Guide

These steps take you from nothing to Flick Battle running on your phone. You'll need a computer, since Unity doesn't run on an iPad.

## What you need

| Item | Cost | Notes |
|---|---|---|
| Unity Personal (Unity 6 LTS) | Free | Free while revenue is under $200K/yr |
| A Mac with Xcode | Free if you own a Mac | **Apple requires a Mac to build iPhone apps.** No Mac? Use Unity Build Automation or Codemagic (cloud Mac builds) |
| Apple Developer account | $99/year | Needed to put the game on an iPhone and on the App Store |
| Google Play Console | $25 one time | Needed to publish on Android |
| Art asset packs | About $20–150 | Ships, explosions, space backgrounds (see "Graphics" below) |

## Step 1: Install Unity
1. Download **Unity Hub** from unity.com/download
2. In Unity Hub go to **Installs → Install Editor**, pick the latest **Unity 6 LTS**
3. Tick these modules: **Android Build Support** (with OpenJDK and SDK/NDK) and **iOS Build Support**

## Step 2: Create the project
1. Unity Hub → **New Project** → pick the **Universal 3D (URP)** template
2. Name it `FlickBattle` and click **Create**
3. Copy the `Assets/Scripts` folder from this repo into your project's `Assets` folder
4. Go to **Edit → Project Settings → Player → Other Settings → Active Input Handling** and set it to **Both**, then let Unity restart

## Step 3: Set up the camera
1. Select **Main Camera** in the Hierarchy
2. Set **Projection** to **Orthographic** and **Size** to `5`
3. Set Position to `(0, 0, -10)` and Rotation to `(0, 0, 0)`
4. **Add Component → Camera Shake**
5. Tick **Post Processing** on the camera (you'll need it for the glow later)

## Step 4: Make the ship prefabs
Use cubes for now. You can swap in real ship models later without touching the code.

1. **GameObject → Create Empty**, name it `BlueShip`
2. Right-click it → **3D Object → Cube**, scale `(0.8, 0.4, 0.4)`. This is a placeholder model. Real ship models must have their **nose pointing right (+X)**
3. Right-click `BlueShip` → **3D Object → Cylinder**, scale `(1.4, 0.02, 1.4)`, rotation `(90, 0, 0)`, name it `SelectionRing`, then untick it so it starts hidden
4. Select `BlueShip` → **Add Component → Ship**. Set **Team** to Blue and drag `SelectionRing` into the **Selection Ring** slot
5. **Add Component → Trail Renderer**, set Time to `0.6`, give it a blue material, and drag it into the Ship's **Trail** slot
6. Drag `BlueShip` from the Hierarchy into the Project window. That makes it a prefab. Then delete it from the Hierarchy
7. Do the same for `RedShip` with **Team = Red**

## Step 5: Game manager and UI
1. **GameObject → Create Empty**, name it `GameManager`
2. Add the components **Game Manager**, **Flick Input** and **Audio Source**
3. Drag the two prefabs into **Blue/Red Ship Prefab** and drag Main Camera into **Camera Shake**
4. **GameObject → UI → Canvas**. On the Canvas Scaler set **Scale With Screen Size**, 1920×1080
5. Add four **UI → Text - TextMeshPro** objects: Turn, Feedback, BlueCount, RedCount. Import the TMP essentials if Unity asks
6. Add two **UI → Button - TextMeshPro**: "New Game" and "Watch Replay"
7. Add the **UI Manager** component to the Canvas and drag the texts and the replay button into its slots
8. Button **On Click ()** → drag in GameManager → pick `GameManager.NewGame` or `GameManager.WatchReplay`
9. Drag the Canvas into GameManager's **UI** slot

Press **Play**. Click a cube, then quickly drag and release. You can test with the mouse in the Editor.

## Step 6: Lock to landscape
**Project Settings → Player → Resolution and Presentation → Default Orientation → Landscape Left**

## Step 7: Test on your phone
- **Android:** turn on Developer Mode and USB debugging on the phone, plug it in, then **File → Build Profiles → Android → Switch Platform → Build And Run**
- **iPhone:** **Build Profiles → iOS → Build**, open the generated Xcode project on a Mac, sign in with your Apple ID, plug in the phone and press Run

---

## Graphics: getting close to R-Type Final 2

R-Type Final 2 was made in Unreal Engine by a professional studio, with custom 3D models and effects. Code alone won't produce that look. You get it from **art assets plus lighting**. Here's how to get close:

1. **Glow (the biggest win):** add a **Global Volume** (GameObject → Volume → Global Volume) and add **Bloom** (Intensity about 1.5, Threshold about 0.9). Give ship engines, lasers and trails **emissive HDR colors**. That glow is most of the R-Type look
2. **3D ship models:** search the Unity Asset Store for "sci-fi spaceship pack" or "space shooter low poly". Pick a sleek fighter style for Blue and a round or organic style for Red, to keep the triangle-vs-circle idea
3. **Explosions:** search "sci-fi explosions" or "Cartoon FX Remaster". Drag an explosion prefab into each Ship's **Explosion Prefab** slot
4. **Background:** use a big nebula skybox or image behind the board, plus a 3D sphere with a gas-giant texture, and a Particle System for drifting stars (slow speed, long lifetime)
5. **Lighting:** one strong colored rim light from behind plus a dark ambient light. That gives the metallic, moody R-Type feel
6. **Sound:** search "sci-fi SFX" on the Asset Store or freesound.org, then drop clips into the GameManager's Whoosh, Explosion, Miss and Win slots

Phones need some care: keep ship models under about 10k triangles each, and use the URP **Mobile** quality setting so the game holds 60 fps.
