export function countHands(frame) {
  const visible=points=>Array.isArray(points)&&points.length===21&&Number.isFinite(points[0]?.confidence)&&points[0].confidence>=.55;
  return Number(visible(frame?.leftHand))+Number(visible(frame?.rightHand));
}
