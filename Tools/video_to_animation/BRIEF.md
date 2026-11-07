# Footage-reading brief (shared by all readers)

Video: D:/arming_sword_refs/arming_sword.mp4 (1920x1080, 60 fps). Golden skeleton with an Arming Sword (one-handed, right hand), Dark and Darker.
On-screen text names the attack ("Attack #1/#2/#3", "Riposte"). Yellow "0.1x Speed" top-right = slow motion; it is ON ONLY during the
active (hitbox-visible, green/red blade overlay) window, the rest of that segment is 1x. Real time: 1 slow frame = 0.1 real frame.

Segments (video frame numbers). Views: A = front-left far 1x, B = right-front 1x, T = top-down 1x, D = right-front with hitbox overlay, active part at 0.1x.
- Attack #1: A 649-756, B 757-864, T 865-978, D 979-1251 (slow 1005-1189)
- Attack #2: A 1252-1349, B 1350-1446, T 1447-1541, D 1542-1816 (slow 1558-1760)
- Attack #3: A 1817-1929, B 1930-2045, T 2046-2161, D 2162-2446 (slow 2190-2381)
- Riposte:   A 2447-2535, B 2536-2629, T 2630-2714, D 2715-2972 (slow 2732-2945)
In view D/B the camera stands at the skeleton's right-front: the skeleton faces roughly image-right, sword arm = right arm.
In T the skeleton faces roughly image-bottom (its right = image-left) — VERIFY per attack from the step direction / blade at peak.

Tools (system python `py`, opencv). Scripts in C:/Users/Raketa/AppData/Local/Temp/claude/69ad9cddeae1e6a4/d1dd5d69-db45-4979-8aea-4e9dfacb34d9/scratchpad/v2a :
- sheet2.py video out.jpg f0 f1 step cols tilew x0,y0,x1,y1(normalized crop) gamma   -> contact sheet, labels frame no. + SLOW
- frames.py video out.jpg cols tilew x0,y0,x1,y1 gamma f1 f2 f3 ...                   -> big crops of chosen frames
Good crops: D/B views 0.3,0.12,0.75,0.75 ; T view 0.33,0.05,0.8,0.7 ; A view: find it. gamma 1.4-1.5.
Use big crops (tilew >= 600, <= 3 cols) for the key frames — small tiles mislead about elbows and wrists. Max image width 1920.

Coordinate frame for the answer: character root space, metres. +Z forward (where the character faces), +X character's right, +Y up, origin between the feet.
Character 1.84 m tall: eye (0, 1.67, 0.11), right shoulder (0.17, 1.47, -0.06), arm length shoulder->wrist ~0.55 m, sword 0.93 m (grip ~0.12, blade ~0.75).
Blade direction: yaw (deg, from +Z toward +X; 90 = pointing to the right, 180 = pointing back, -90 = left) and elevation (deg, + up).

Deliver (write to keys/<ATTACK>/report.md, and save 4-6 key-frame crops you used as keys/<ATTACK>/<phase>_<view>_f<frame>.jpg, side-by-side views where possible):
1. Timing in REAL 60 fps frames: idle->windup start, windup peak (fully cocked), active start/end (hitbox on), visual peak/contact moment, recovery end. Cross-check with the 1x views.
2. Per phase (idle, windup-cocked, active start, PEAK, active end/follow-through, recovery): hand (wrist) position xyz, blade yaw/elevation,
   elbow position xyz and its side (up/out/down/behind), palm facing (up/down/in/out) and knuckles, edge leading direction, forearm roll,
   torso yaw/pitch (deg, + = turned right / leaning forward), head, left (off) hand position, legs/step (which foot, how far), hips.
3. Trajectory of the tip and of the hand as a short path description + 6-10 sampled tip positions (xyz).
4. Peak relative to the eye level: tip height and hand height vs eye (1.67), distance ahead.
5. Grip: how the fist holds the hilt (hammer grip / thumb on the guard / blade lean relative to forearm in deg), changes during the swing.
6. Confidence notes: what is ambiguous, which view disagrees.
Do NOT edit anything in the Unity project. Be concrete and numeric; ~80 lines max in the reply (the report file can be longer).
