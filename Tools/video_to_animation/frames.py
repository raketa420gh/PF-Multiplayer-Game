import cv2,sys,numpy as np
# frames.py video out cols tilew crop gamma f1 f2 ...
v,out,cols,tw,cr,gam=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]),[float(x) for x in sys.argv[5].split(',')],float(sys.argv[6])
lut=np.array([((i/255)**(1/gam))*255 for i in range(256)],np.uint8);c=cv2.VideoCapture(v);tiles=[]
for fr in map(int,sys.argv[7:]):
    c.set(1,fr);ok,f=c.read();h,w=f.shape[:2]
    f=cv2.LUT(f[int(cr[1]*h):int(cr[3]*h),int(cr[0]*w):int(cr[2]*w)],lut)
    th=int(f.shape[0]*tw/f.shape[1]);f=cv2.resize(f,(tw,th),interpolation=cv2.INTER_AREA)
    cv2.putText(f,f"f{fr}",(4,th-6),0,0.7,(0,0,0),3);cv2.putText(f,f"f{fr}",(4,th-6),0,0.7,(0,255,255),1);tiles.append(f)
while len(tiles)%cols:tiles.append(np.zeros_like(tiles[0]))
cv2.imwrite(out,np.vstack([np.hstack(tiles[i:i+cols]) for i in range(0,len(tiles),cols)]))
