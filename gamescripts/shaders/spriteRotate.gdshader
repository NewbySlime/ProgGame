shader_type canvas_item;

const vec2 offsetPos = vec2(0.5, 0.5);

void fragment(){
	vec2 offsetvec = UV-offsetPos;
	float distfromcenter = length(offsetvec);
	vec2 anglevec = normalize(offsetvec);
	float newangle = atan(anglevec.y, anglevec.x) - TIME;
	anglevec = vec2(cos(newangle), sin(newangle));
	vec2 newuv = (distfromcenter * anglevec) + offsetPos;
	if(newuv.x <= 1.f && newuv.x > 0.f && newuv.y <= 1.f && newuv.y > 0.f)
		COLOR = texture(TEXTURE, (distfromcenter*anglevec)+offsetPos);
	else
		COLOR = vec4(0);
}