shader_type canvas_item;

uniform sampler2D noise;

const vec2 offset_b = vec2(0.03, 0.04);
const vec2 offset = vec2(0.02, 0.03);
const float off = 0.2;

vec4 glitch(vec2 offsetv, vec4 multiplyer, vec2 UVpos){
	vec4 currcol = texture(noise, UVpos), res = vec4(0);
	if(currcol.x < off){
		currcol = texture(noise, UVpos+offsetv);
		if(currcol.x < off){
			res = multiplyer;
		}
	}

	currcol = texture(noise, UVpos+offsetv);
	if(currcol.x < off){
		res = multiplyer;
	}
	
	return res;
}

void fragment(){
	COLOR = texture(noise, UV);
	
	if(COLOR.x > off){
		COLOR = texture(TEXTURE, UV);
	}
	else{
		COLOR = texture(noise, UV+offset);
		if(COLOR.x < off){
			COLOR = texture(TEXTURE, UV+offset);
		}else{
			COLOR = vec4(0);
		}
		//COLOR = texture(TEXTURE, UV);
	}
	
	vec4 currcol = texture(noise, UV+offset);
	if(currcol.x < off){
		COLOR = texture(TEXTURE, UV-offset);
	}
	
	// need some changes
	currcol = glitch(offset+vec2(0.01), vec4(0f,0f,1f,0f), UV);
	if(currcol.b == 1f)
		COLOR.b = 1f;
	else{
		currcol = glitch(offset+vec2(0f,0.02), vec4(0f,1f,0f,0f), UV);
		if(currcol.g == 1f)
			COLOR.g = 1f;
		else{
			currcol = glitch(offset-vec2(0.01), vec4(1f,0f,0f,0f), UV);
			if(currcol.r == 1f)
				COLOR.r = 1f;
		}
	}
}