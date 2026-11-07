import cv2,sys,numpy as np
c=cv2.VideoCapture(sys.argv[1]);prev=None;i=0;fps=c.get(5)
while True:
    ok,f=c.read()
    if not ok:break
    g=cv2.resize(cv2.cvtColor(f,cv2.COLOR_BGR2GRAY),(96,54)).astype(np.float32)
    # slow-speed marker: top-right region brightness
    if prev is not None:
        d=np.abs(g-prev).mean()
        if d>25:print(f"cut f{i} {i/fps:.2f}s d={d:.0f}")
    prev=g;i+=1
