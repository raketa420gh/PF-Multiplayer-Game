import cv2,sys,os,json,numpy as np
# Writes real-time 60 fps jpg sequences of every footage view into html/ref/<atk>/<view>/NNN.jpg
V='D:/arming_sword_refs/arming_sword.mp4';out=sys.argv[1]
SEG={'A1':{'A':(649,756),'B':(757,864),'T':(865,978),'D':(979,1251,1005,1189)},
     'A2':{'A':(1252,1349),'B':(1350,1446),'T':(1447,1541),'D':(1542,1816,1558,1760)},
     'A3':{'A':(1817,1929),'B':(1930,2045),'T':(2046,2161),'D':(2162,2446,2190,2381)},
     'R': {'A':(2447,2535),'B':(2536,2629),'T':(2630,2714),'D':(2715,2972,2732,2945)}}
CROP={'A':(0.28,0.0,0.78,1.0),'B':(0.22,0.0,0.82,1.0),'T':(0.3,0.0,0.85,0.75),'D':(0.22,0.0,0.82,1.0)}
lut=np.array([((i/255)**(1/1.4))*255 for i in range(256)],np.uint8)
c=cv2.VideoCapture(V);meta={}
for atk,views in SEG.items():
    meta[atk]={}
    for v,s in views.items():
        frames=list(range(s[0],s[1]+1))
        if len(s)>2: frames=[f for f in frames if f<s[2] or f>s[3] or (f-s[2])%10==0]
        d=f'{out}/{atk}/{v}';os.makedirs(d,exist_ok=True);cr=CROP[v]
        for k,f in enumerate(frames):
            c.set(1,f);ok,im=c.read();h,w=im.shape[:2]
            im=cv2.LUT(im[int(cr[1]*h):int(cr[3]*h),int(cr[0]*w):int(cr[2]*w)],lut)
            im=cv2.resize(im,(int(im.shape[1]*420/im.shape[0]),420),interpolation=cv2.INTER_AREA)
            cv2.imwrite(f'{d}/{k:03d}.jpg',im,[1,80])
        meta[atk][v]=frames
json.dump(meta,open(f'{out}/frames.json','w'))
