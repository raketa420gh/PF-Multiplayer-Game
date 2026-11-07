import cv2,sys,numpy as np
# sheet2.py video out f0 f1 step cols tilew crop(x0,y0,x1,y1 norm) [gamma]
v,out=sys.argv[1],sys.argv[2];f0,f1,st,cols,tw=map(int,sys.argv[3:8])
cr=[float(x) for x in sys.argv[8].split(',')];gam=float(sys.argv[9]) if len(sys.argv)>9 else 1
lut=np.array([((i/255)**(1/gam))*255 for i in range(256)],np.uint8)
c=cv2.VideoCapture(v);tiles=[]
c.set(1,f0);i=f0
while i<=f1:
    ok,f=c.read()
    if not ok:break
    if (i-f0)%st==0:
        h,w=f.shape[:2]
        tr=f[0:60,int(w*0.88):w];slow=((tr[:,:,2]>180)&(tr[:,:,1]>180)&(tr[:,:,0]<120)).sum()>80
        f=cv2.LUT(f[int(cr[1]*h):int(cr[3]*h),int(cr[0]*w):int(cr[2]*w)],lut)
        th=int(f.shape[0]*tw/f.shape[1]);f=cv2.resize(f,(tw,th),interpolation=cv2.INTER_AREA)
        lab=f"f{i}"+(" SLOW" if slow else "");y=th-6
        cv2.putText(f,lab,(4,y),0,0.6,(0,0,0),3);cv2.putText(f,lab,(4,y),0,0.6,(0,255,255),1)
        tiles.append(f)
    i+=1
while len(tiles)%cols:tiles.append(np.zeros_like(tiles[0]))
cv2.imwrite(out,np.vstack([np.hstack(tiles[i:i+cols]) for i in range(0,len(tiles),cols)]));print(out,len(tiles))
