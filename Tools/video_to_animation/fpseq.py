import cv2,sys,os,numpy as np
# fpseq.py out_dir f0 f1 : real-time 60 fps jpg sequence of the FP footage (1x part), same look as refseq.py
V='D:/arming_sword_refs/arming_sword_all_attacks.mp4';out,f0,f1=sys.argv[1],int(sys.argv[2]),int(sys.argv[3])
lut=np.array([((i/255)**(1/1.4))*255 for i in range(256)],np.uint8);c=cv2.VideoCapture(V);os.makedirs(out,exist_ok=True)
for k,f in enumerate(range(f0,f1+1)):
    c.set(1,f);ok,im=c.read();im=cv2.LUT(im,lut)
    cv2.imwrite(f'{out}/{k:03d}.jpg',cv2.resize(im,(747,420),interpolation=cv2.INTER_AREA),[1,80])
