shader_type canvas_item;

const vec2 offsetPos = vec2(0.5, 0.5);

void fragment(){
	vec2 offsetvec = UV-offsetPos;
	float distfromcenter = length(offsetvec);
	vec2 anglevec = normalize(offsetvec);
	float newangle = atan(anglevec.y, anglevec.x) - TIME;
	anglevec = vec2(cos(newangle), sin(newangle));
	COLOR = texture(TEXTURE, (distfromcenter*anglevec)+offsetPos);
}