import cv2,sys,numpy as np
# oursheet.py dir swing view out f0 f1 step
d,s,v,out,f0,f1,st=sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4],*map(int,sys.argv[5:8])
t=[]
for f in range(f0,f1+1,st):
    im=cv2.imread(f'{d}/g_{s}_{v}_{f}.png')
    if im is None:continue
    im=cv2.resize(im,(240,int(im.shape[0]*240/im.shape[1])));cv2.putText(im,str(f),(4,20),0,0.6,(0,255,255),1);t.append(im)
while len(t)%8:t.append(np.zeros_like(t[0]))
cv2.imwrite(out,np.vstack([np.hstack(t[i:i+8]) for i in range(0,len(t),8)]))
