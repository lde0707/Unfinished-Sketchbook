"""Connected-component extraction for irregularly spaced generated sprite atlases."""
import numpy as np
from PIL import Image

def split_frames(im):
 rgba=np.array(im);h,w=rgba.shape[:2];rows=3 if w/h>1.2 else 4;runs=[];parents=[];previous=[]
 def root(i):
  while parents[i]!=i:
   parents[i]=parents[parents[i]];i=parents[i]
  return i
 for y,row in enumerate(rgba[:,:,3]>32):
  edges=np.flatnonzero(np.diff(np.pad(row.astype(np.int8),(1,1))))
  current=[];p=0
  for a,b in edges.reshape(-1,2):
   index=len(runs);parents.append(index);runs.append((y,int(a),int(b)));current.append(index)
   while p<len(previous) and runs[previous[p]][2]<a:p+=1
   q=p
   while q<len(previous) and runs[previous[q]][1]<=b:
    other=root(previous[q]);mine=root(index)
    if mine!=other:parents[mine]=other
    q+=1
  previous=current
 components={}
 for i,(y,a,b) in enumerate(runs):components.setdefault(root(i),[]).append((y,a,b))
 groups=[[] for _ in range(rows*4)]
 for rr in components.values():
  if sum(b-a for y,a,b in rr)<10:continue
  x1=min(a for y,a,b in rr);x2=max(b for y,a,b in rr);y1=rr[0][0];y2=rr[-1][0]+1
  cell=min(rows-1,int((y1+y2)/2/(h/rows)))*4+min(3,int((x1+x2)/2/(w/4)))
  groups[cell].append(rr)
 frames=[]
 for i,parts in enumerate(groups):
  if rows==4 and 8<=i<12:continue
  # Detached decorative flecks must not contaminate the neighboring recovery frame.
  if i<8:parts=[max(parts,key=lambda rr:sum(b-a for y,a,b in rr))]
  rr=[run for part in parts for run in part]
  assert rr,('empty frame',i)
  x1=max(0,min(a for y,a,b in rr)-1);x2=min(w,max(b for y,a,b in rr)+1)
  y1=max(0,min(y for y,a,b in rr)-1);y2=min(h,max(y for y,a,b in rr)+2)
  cut=rgba[y1:y2,x1:x2].copy();mask=np.zeros(cut.shape[:2],dtype=bool)
  for y,a,b in rr:mask[max(0,y-y1-1):min(y2-y1,y-y1+2),max(0,a-x1-1):min(x2-x1,b-x1+1)]=True
  cut[:,:,3]=np.where(mask,cut[:,:,3],0);frames.append(Image.fromarray(cut))
 return frames
