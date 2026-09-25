"""Encode rendered Unity frames, preserving the source frame rate and count."""
import json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tmp/video-deps'))
import imageio_ffmpeg
folder=ROOT/'output/campaign'
meta=json.loads((folder/'recording.json').read_text(encoding='utf-8-sig'))
assert all((folder/'frames'/f'frame-{i:04}.png').is_file() for i in range(meta['frames']))
output=folder/'campaign-opening.mp4'
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
subprocess.run([ffmpeg,'-y','-hide_banner','-loglevel','warning','-framerate',str(meta['fps']),'-i',str(folder/'frames/frame-%04d.png'),'-frames:v',str(meta['frames']),'-c:v','libx264','-preset','medium','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True)
subprocess.run([ffmpeg,'-v','error','-i',str(output),'-f','null','-'],check=True)
meta.update(file=output.name,bytes=output.stat().st_size,width=1600,height=1000,audio=False)
(folder/'video-report.json').write_text(json.dumps(meta,indent=2)+'\n',encoding='utf-8')
print(json.dumps(meta,indent=2))
