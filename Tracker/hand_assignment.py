def anatomical_hand(category, wrist, pose):
    """Camera input is unmirrored. Prefer anatomical pose wrists over selfie labels."""
    if pose and len(pose) > 16:
        left, right = pose[15], pose[16]
        if min(left.visibility, right.visibility) > .5:
            dl = (wrist.x-left.x)**2 + (wrist.y-left.y)**2
            dr = (wrist.x-right.x)**2 + (wrist.y-right.y)**2
            return 'leftHand' if dl < dr else 'rightHand'
    return 'rightHand' if category == 'Left' else 'leftHand'
