shader_type canvas_item;

uniform float test;

const float max_y = 1f;
const float min_y = 0.5f;

void fragment(){
	float texpos = 0f;
	vec2 pos = vec2((TIME * 0.05) + UV.x, UV.y);
	COLOR = texture(TEXTURE, pos).xyzz;
	if(COLOR.z < test){
		COLOR.a = 0f;
	}
	
	float currentypos = max(UV.y, 1f-UV.y);
	if(min_y < currentypos){
		COLOR.w = (1f-(currentypos-min_y)/(max_y-min_y)) * COLOR.w;
	}
}