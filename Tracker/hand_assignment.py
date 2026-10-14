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
