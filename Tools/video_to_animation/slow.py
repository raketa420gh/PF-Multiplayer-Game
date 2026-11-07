import cv2,sys
c=cv2.VideoCapture(sys.argv[1]);i=0;st=None
while True:
    ok,f=c.read()
    if not ok:break
    w=f.shape[1];tr=f[0:60,int(w*0.88):w];s=((tr[:,:,2]>180)&(tr[:,:,1]>180)&(tr[:,:,0]<120)).sum()>80
    if s and st is None:st=i
    if not s and st is not None:print(f"slow {st}-{i-1} ({i-st} frames)");st=None
    i+=1
