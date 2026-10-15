def anatomical_hand(category, wrist, pose):
    """Camera input is unmirrored. Prefer anatomical pose wrists over selfie labels."""
    if pose and len(pose) > 16:
        left, right = pose[15], pose[16]
        if min(left.visibility, right.visibility) > .5:
            dl = (wrist.x-left.x)**2 + (wrist.y-left.y)**2
            dr = (wrist.x-right.x)**2 + (wrist.y-right.y)**2
            return 'leftHand' if dl < dr else 'rightHand'
        # A seated webcam often sees only one wrist. Do not switch its
        # anatomical side merely because the other arm is outside the image.
        for side, point in [('leftHand', left), ('rightHand', right)]:
            if point.visibility > .5 and 0 <= point.x <= 1 and 0 <= point.y <= 1:
                if (wrist.x-point.x)**2 + (wrist.y-point.y)**2 < .15**2:
                    return side
    return 'rightHand' if category == 'Left' else 'leftHand'

def assign_hands(categories, wrists, pose):
    """Joint two-hand assignment: nearby wrists must never overwrite one another."""
    if len(wrists)!=2:return [anatomical_hand(c,w,pose) for c,w in zip(categories,wrists)]
    if pose and len(pose)>16 and min(pose[15].visibility,pose[16].visibility)>.5:
        def distance(a,b):return (a.x-b.x)**2+(a.y-b.y)**2
        direct=distance(wrists[0],pose[15])+distance(wrists[1],pose[16])
        swapped=distance(wrists[0],pose[16])+distance(wrists[1],pose[15])
        if abs(direct-swapped)>.0001:
            return ['leftHand','rightHand'] if direct<swapped else ['rightHand','leftHand']
    # Ambiguous contact/occlusion: handedness is preferable to a duplicate side.
    first='rightHand' if categories[0]=='Left' else 'leftHand'
    return [first,'leftHand' if first=='rightHand' else 'rightHand']

def hands_together(hands):
    """Image-space contact cue; monocular world depth is unreliable at prayer contact."""
    import math
    if len(hands)!=2 or any(len(h)<21 for h in hands):return False
    centers=[];lengths=[];ups=[]
    for h in hands:
        centers.append((sum(h[i].x for i in (5,9,13,17))/4,sum(h[i].y for i in (5,9,13,17))/4))
        dx,dy=h[9].x-h[0].x,h[9].y-h[0].y;length=math.hypot(dx,dy)
        if length<.015 or dy>-.01:return False
        lengths.append(length);ups.append((dx/length,dy/length))
    return sum(a*b for a,b in zip(*ups))>.75 and math.dist(*centers)<sum(lengths)*.45
