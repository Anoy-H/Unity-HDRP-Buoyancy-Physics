# Unity-HDRP-Buoyancy-Physics
A simple buoyancy physics solution for Unity's High Definition Render Pipeline (HDRP). This package allows you to simulate floating objects if you use the default HDRP water.


https://github.com/user-attachments/assets/4a70c1b1-a906-4691-a78b-f2d0bc29d538


########################### Setup Guide ###############################################

Thank you for downloading the package. To make your game objects float, you can choose between two primary setup methods depending on your requirements.

# Important: Tested Unity Editor version 6000+, older versions may not work. Also, you must enable the Scripting option from the water settings.

**At first, download the .unityasset file from the releases section and import it to your project or download the whole demo unity project by downloading the repository.**

**Method 1: Single Mesh Buoyancy**

(1) Attach a Rigidbody component to your main mesh object and configure the mass as required.

(2) Add the HDRPBuoyancy.cs script to the same object.

(3) Drag the object’s own Rigidbody into the corresponding slot on the script in the Inspector.


**Method 2: Multi Floating Point Buoyancy**

(1) Add a Rigidbody to your main parent object and configure the mass as required.

(2) Create mesh based GameObjects (for example: cubes) as the children of the parent and position them where you want to apply the buoyant force (usually in the corners of the parent object).

(3) Add the HDRPBuoyancy.cs script to each child floating point GameObjects.

(4) For every child object's HDRPBuoyancy script, drag the Parent's Rigidbody into the script’s Rigidbody slot in the Inspector.

**You can deactivate the Mesh Renderers of every child floating point objects to keep them invisible.**

# Support

You’re all set. If you run into any issues or have questions, feel free to reach out.

**Anoy Howlader**

**Contact E-mail: anoyh12@gmail.com**

**To support me through your little contributions: https://www.patreon.com/cw/anoyh12/membership**
