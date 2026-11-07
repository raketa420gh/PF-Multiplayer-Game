import cv2,sys,numpy as np
# sheet.py video out start_s end_s step_s cols tilew [crop x0,y0,x1,y1 normalized]
v,out,a,b,st,cols,tw=sys.argv[1],sys.argv[2],float(sys.argv[3]),float(sys.argv[4]),float(sys.argv[5]),int(sys.argv[6]),int(sys.argv[7])
crop=[float(x) for x in sys.argv[8].split(',')] if len(sys.argv)>8 else [0,0,1,1]
c=cv2.VideoCapture(v);fps=c.get(5);tiles=[]
t=a
while t<=b+1e-6:
    c.set(cv2.CAP_PROP_POS_FRAMES,round(t*fps));ok,f=c.read()
    if not ok:break
    h,w=f.shape[:2];f=f[int(crop[1]*h):int(crop[3]*h),int(crop[0]*w):int(crop[2]*w)]
    th=int(f.shape[0]*tw/f.shape[1]);f=cv2.resize(f,(tw,th),interpolation=cv2.INTER_AREA)
    lab=f"{t:.2f}s f{round(t*fps)}";cv2.putText(f,lab,(4,18),0,0.55,(0,0,0),3);cv2.putText(f,lab,(4,18),0,0.55,(0,255,255),1)
    tiles.append(f);t+=st
while len(tiles)%cols:tiles.append(np.zeros_like(tiles[0]))
rows=[np.hstack(tiles[i:i+cols]) for i in range(0,len(tiles),cols)]
cv2.imwrite(out,np.vstack(rows));print(out,len(tiles))
