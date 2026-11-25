# -*- coding: utf-8 -*-
# Scheme A3 (simplified): three diagrams — zones / asset layout / movement flow.
# Coordinates are Unity world metres (x east, z north). Arena = rectangle x[-21,18] z[-7.1,25.1].
from PIL import Image, ImageDraw, ImageFont
import os, math, random

OUT = os.path.dirname(os.path.abspath(__file__))
S = 20
VX0, VX1, VZ0, VZ1 = -24.0, 21.0, -10.0, 28.0        # visible window (m)
MW, MH = int((VX1 - VX0) * S), int((VZ1 - VZ0) * S)
PANEL = 400
FONT = "C:/Windows/Fonts/msyh.ttc"
fs, fm, fl, fx = [ImageFont.truetype(FONT, n) for n in (13, 16, 22, 30)]

def P(x, z): return ((x - VX0) * S, (VZ1 - z) * S)

AX0, AX1, AZ0, AZ1 = -21.0, 18.0, -7.1, 25.1           # arena
BED = (-0.2, -7.1, 18.0, 1.5)                          # bed platform, top ~3.6m
CH = {                                                  # corner chamfer triangles (5m legs)
    "NW": [(-21, 25.1), (-16, 25.1), (-21, 20.1)],
    "NE": [(18, 25.1), (13, 25.1), (18, 20.1)],
    "SW": [(-21, -7.1), (-16, -7.1), (-21, -2.1)],
    "SB": [(-0.2, -7.1), (-5.2, -7.1), (-0.2, -2.1)],
}
BOXES = {"NW": (-18.4, 22.4), "NE": (15.4, 22.4), "SW": (-18.4, -4.6), "SB": (-2.8, -4.6)}
RAMP1 = (0.0, 1.5, 9.7, 4.9)     # rises west -> east
RAMP2 = (9.7, 1.5, 18.0, 4.9)    # descends west -> east
IW = (-14.6, 3.8, -4.9, 12.7)    # Objects_Grp_07 + 11 (stay)
IN1 = (-3.8, 15.2, -0.2, 18.8)   # Cardboard_Box (moved)
IN2 = (9.2, 14.2, 12.8, 17.8)    # Cardboard_Box (moved)
SPAWN = (-8, 0)
CANNON = (15.2, -5.0)
TRAMP = (-3.5, -2.0)

def canvas():
    im = Image.new("RGB", (MW + PANEL, MH), (246, 244, 239))
    d = ImageDraw.Draw(im, "RGBA")
    d.rectangle([0, 0, MW, MH], fill=(120, 118, 114))                       # outside = dark
    a, b = P(AX0, AZ1); c, e = P(AX1, AZ0)
    d.rectangle([a, b, c, e], fill=(238, 232, 216))                         # arena floor
    for gx in range(-20, 19, 8): d.line([P(gx, AZ0), P(gx, AZ1)], fill=(224, 217, 200), width=1)
    for gz in range(-4, 26, 8): d.line([P(AX0, gz), P(AX1, gz)], fill=(224, 217, 200), width=1)
    d.text(P(-23.5, 27.5), "场外(不可进入,保留为背景)", font=fs, fill=(215, 215, 215))
    return im, d

def rect(d, r, fill, outline=None, w=2):
    d.rectangle([P(r[0], r[3]), P(r[2], r[1])], fill=fill, outline=outline, width=w)

def tag(d, xy, text, fg=(40, 40, 40), font=None, bg=(255, 255, 255, 220)):
    font = font or fs
    b = d.multiline_textbbox(xy, text, font=font)
    d.rectangle([b[0] - 3, b[1] - 2, b[2] + 3, b[3] + 2], fill=bg)
    d.multiline_text(xy, text, font=font, fill=fg)

def tri(d, pts, fill, outline=None):
    d.polygon([P(*p) for p in pts], fill=fill, outline=outline)

def arrow(d, pts, col, w=5, head=True):
    d.line([P(*p) for p in pts], fill=col, width=w, joint="curve")
    if head:
        (x1, z1), (x2, z2) = pts[-2], pts[-1]
        ex, ey = P(x2, z2); sx, sy = P(x1, z1)
        ang = math.atan2(ey - sy, ex - sx)
        for da in (2.6, -2.6):
            d.line([(ex, ey), (ex + 16 * math.cos(ang + da), ey + 16 * math.sin(ang + da))], fill=col, width=w)

def airwall(d):
    pts = [(AX0, AZ0), (AX1, AZ0), (AX1, AZ1), (AX0, AZ1), (AX0, AZ0)]
    d.line([P(*p) for p in pts], fill=(220, 30, 30), width=4)

def toybox(d, x, z, big=False):
    px, py = P(x, z); r = 13
    d.rectangle([px - r, py - r, px + r, py + r], fill=(150, 95, 50), outline=(70, 40, 10), width=3)
    d.text((px, py), "箱", font=fm, fill="white", anchor="mm")

def player(d, x, z):
    px, py = P(x, z)
    d.ellipse([px - 13, py - 13, px + 13, py + 13], fill=(40, 110, 240), outline="white", width=3)
    d.text((px, py), "主", font=fm, fill="white", anchor="mm")

def cannon(d, x, z):
    px, py = P(x, z)
    d.polygon([(px, py - 16), (px + 15, py + 12), (px - 15, py + 12)], fill=(230, 60, 60), outline="white")
    d.text((px, py + 3), "炮", font=fs, fill="white", anchor="mm")

def panel(im, title, sub, sections, foot=None):
    d = ImageDraw.Draw(im, "RGBA")
    x = MW + 18; y = 16
    d.text((x, y), title, font=fx, fill=(30, 30, 30)); y += 44
    d.text((x, y), sub, font=fm, fill=(90, 90, 90)); y += 34
    for head, items in sections:
        d.text((x, y), head, font=fl, fill=(30, 60, 150)); y += 32
        for it in items:
            cur = ""
            for ch in it:
                if d.textlength(cur + ch, font=fs) > PANEL - 36:
                    d.text((x, y), cur, font=fs, fill=(40, 40, 40)); y += 20; cur = ch
                else: cur += ch
            d.text((x, y), cur, font=fs, fill=(40, 40, 40)); y += 24
        y += 8
    if foot: d.text((x, MH - 24), foot, font=fs, fill=(130, 130, 130))
    d.text((10, MH - 22), "N ↑   格线 8m   示意图,坐标进场景后微调", font=fs, fill=(230, 230, 230))

# ---------------------------------------------------------------- 1 zones
def img1():
    im, d = canvas()
    rect(d, (-16, 5, 17, 23.5), (40, 140, 210, 70), (40, 140, 210), 3)
    tag(d, P(-15.5, 23.2), "① 主战区 · 开阔  ≈ 800m²", fg=(20, 100, 170), font=fm)
    rect(d, (AX0, AZ0, -0.2, 5), (60, 170, 90, 60), (60, 170, 90), 3)
    tag(d, P(-20.6, 4.6), "③ 西侧绕行带", fg=(30, 120, 60), font=fm)
    rect(d, BED, (230, 140, 20, 90), (210, 120, 0), 3)
    tag(d, P(0.6, 1.1), "② 二层 · 床顶平台 18×8.6m ≈ 155m²", fg=(170, 90, 0), font=fm)
    rect(d, (RAMP1[0], RAMP1[1], RAMP2[2], RAMP1[3]), (170, 90, 200, 90), (130, 60, 170), 3)
    tag(d, P(2.5, 4.6), "②′ 人字坡(上二层通道)", fg=(110, 40, 150), font=fm)
    for k, pts in CH.items():
        tri(d, pts, (150, 95, 50, 190), (90, 55, 20))
    for k, (x, z) in BOXES.items(): toybox(d, x, z)
    tag(d, P(-20.6, 23.6), "④ 出羊角", fg=(110, 60, 20));
    for r in (IW, IN1, IN2): rect(d, r, (120, 120, 120, 150), (70, 70, 70), 2)
    tag(d, P(-14, 8.5), "⑤ 掩体岛", fg=(60, 60, 60))
    rect(d, BED, None, None, 0)
    airwall(d)
    tag(d, P(AX0, AZ1 + 1.2), "空气墙(红线):沿场地一圈 + 天花板", fg=(200, 30, 30))
    panel(im, "1 · 大功能分区", "A3 · 简化版,全部围合在场地内",
          [("分区", ["① 主战区(蓝):中部和北部整块清空,只留零散小玩具和 3 个掩体岛,最大最开阔。",
                    "② 二层(橙):床顶平台,次级大平台。",
                    "②′ 人字坡(紫):贴着床的北侧,一上一下,是上下二层的唯一通道。",
                    "③ 西侧绕行带(绿):连接坡脚和主战区,让地面可以绕回来。",
                    "④ 出羊角(棕):4 个斜角封堵 + 玩具箱,野羊从这里出来。",
                    "⑤ 掩体岛(灰):3 个孤立岛,四周都留 4m 以上通道。"]),
           ("场地范围", ["39 × 32m 的长方形(x -21~18,z -7.1~25.1);南侧贴着床边立墙,书桌、衣柜都不进场。"])])
    im.save(os.path.join(OUT, "A3_1_Zones.png"))

# ---------------------------------------------------------------- 2 assets
def img2():
    im, d = canvas()
    # faded out-of-play furniture
    for (n, r) in (("书桌A", (-1.6, -18.5, 5.0, -13.3)), ("书桌B", (-12.5, -18.6, -4.7, -11.8)), ("衣柜A", (-25.0, -15.2, -19.4, -7.5)), ("衣柜B", (5.9, -21.4, 13.6, -15.7)), ("床头柜", (14.8, -12.6, 17.7, -9.0))):
        rect(d, r, (95, 93, 90), (70, 68, 66), 1)
        px, py = P((r[0] + r[2]) / 2, (r[1] + r[3]) / 2); d.text((px, py), n + "\n(场外)", font=fs, fill=(190, 190, 190), anchor="mm", align="center")
    rect(d, BED, (200, 170, 120), (140, 105, 50), 3)
    tag(d, P(6, -3.2), "Bed_Grp(原位,二层平台)", fg=(100, 70, 20))
    # ramps (Board_03_Long x2)
    rect(d, RAMP1, (150, 130, 190), (100, 70, 150), 2); rect(d, RAMP2, (150, 130, 190), (100, 70, 150), 2)
    arrow(d, [(1.0, 3.2), (8.5, 3.2)], (70, 40, 130), 3); arrow(d, [(11.0, 3.2), (17.0, 3.2)], (70, 40, 130), 3)
    tag(d, P(0.2, 6.6), "Board_03_Long ×2 复制 → 人字坡(贴床北侧,上 3.6m)", fg=(90, 50, 140))
    # chamfers + boxes
    for k, pts in CH.items(): tri(d, pts, (140, 120, 90, 230), (80, 60, 30))
    tag(d, P(-20.8, 18.6), "斜角封堵:\nBook_Pile / Modular_Cube_Pile\n/ 纸箱 堆叠成斜面", fg=(90, 60, 30))
    for k, (x, z) in BOXES.items(): toybox(d, x, z)
    tag(d, P(-20.8, 2.6), "Cardboard_Box_01/02 ×4\n= 玩具箱(出羊)", fg=(110, 60, 20))
    # islands
    rect(d, IW, (190, 180, 160), (110, 100, 80), 2); tag(d, P(-14.2, 8.8), "Objects_Grp_07+11\n(原位保留)", fg=(60, 55, 45))
    rect(d, IN1, (200, 170, 120), (110, 80, 40), 2); rect(d, IN2, (200, 170, 120), (110, 80, 40), 2)
    tag(d, P(-3.6, 21.0), "Cardboard_Box\n(移到这)", fg=(100, 70, 30)); tag(d, P(9.4, 20.0), "Cardboard_Box\n(移到这)", fg=(100, 70, 30))
    # scattered small toys
    rnd = random.Random(5)
    cols = [(230, 90, 90), (90, 160, 230), (240, 190, 60), (110, 200, 120), (190, 110, 220)]
    avoid = [IW, IN1, IN2, BED, RAMP1, RAMP2]
    n = 0
    while n < 44:
        x = rnd.uniform(-19.5, 17); z = rnd.uniform(-5.5, 24)
        if any(a - 0.9 < x < c + 0.9 and b - 0.9 < z < e + 0.9 for (a, b, c, e) in avoid): continue
        if x < -14 and z > 19: continue
        if x > 12 and z > 21: continue
        if z < -1 and (x < -14 or -6 < x < 0): continue
        w = rnd.uniform(0.6, 1.2)
        d.rectangle([P(x - w / 2, z + w / 2), P(x + w / 2, z - w / 2)], fill=rnd.choice(cols), outline=(70, 70, 70)); n += 1
    tag(d, P(-3, 12.0), "PF_Cube_Toys / PF_Pen 物理小件 ×约 44\n(从现有 66 个里挑,打散)", fg=(30, 70, 120))
    # trampoline + cannon + spawn
    px, py = P(*TRAMP); d.ellipse([px - 9, py - 9, px + 9, py + 9], fill=(255, 150, 60), outline=(150, 70, 0), width=2)
    tag(d, (px + 12, py - 8), "弹床(挪来)", fg=(150, 70, 0))
    cannon(d, *CANNON); tag(d, P(9.0, -4.6), "大炮(待做,项目里没找到)", fg=(200, 30, 30))
    player(d, *SPAWN)
    airwall(d)
    panel(im, "2 · 场地资产分布", "A3 · 用现有资产重组",
          [("保留原位", ["Bed_Grp(床)", "Objects_Grp_07 + 11(西侧掩体岛)", "Road_03_Bend_02 等地面装饰"]),
           ("复制 / 挪用", ["Board_03_Long ×2 → 人字坡", "Cardboard_Box ×6 → 4 玩具箱 + 2 掩体岛", "Book_Pile / Modular_Cube_Pile → 斜角封堵", "PF_Cube_Toys / PF_Pen → 散落玩具", "弹床(现 (6.5,4.35))→ 挪到床西侧"]),
           ("清出 / 隐藏", ["Objects_Grp_01/02/03/14/15/16(中部大堆)", "书桌、衣柜、床头柜、旧赛道路标(场外)"]),
           ("要新做", ["大炮(反击点),项目里没找到,需要新做或找资产", "斜角封堵的摆放"])])
    im.save(os.path.join(OUT, "A3_2_Assets.png"))

# ---------------------------------------------------------------- 3 flow
def img3():
    im, d = canvas()
    rect(d, BED, (230, 140, 20, 60), (210, 120, 0), 2)
    rect(d, RAMP1, (170, 90, 200, 70), (130, 60, 170), 2); rect(d, RAMP2, (170, 90, 200, 70), (130, 60, 170), 2)
    for k, pts in CH.items(): tri(d, pts, (150, 95, 50, 190), (90, 55, 20))
    for r in (IW, IN1, IN2): rect(d, r, (120, 120, 120, 150), (70, 70, 70), 2)
    for k, (x, z) in BOXES.items(): toybox(d, x, z)
    # sheep spawn flows
    G = (60, 160, 80)
    arrow(d, [(-17.3, 21.3), (-12, 17)], G, 3); arrow(d, [(14.3, 21.3), (12, 18)], G, 3)
    arrow(d, [(-17.3, -3.5), (-12, 0)], G, 3); arrow(d, [(-2.8, -3.2), (-6.5, 1)], G, 3)
    tag(d, P(-12.5, 18.4), "野羊从 4 个角涌入中央", fg=(30, 110, 50))
    # ground ring (blue)
    B = (40, 90, 220)
    ring = [(-18, -2), (-18, 17), (-13, 22.5), (13, 22.5), (16.5, 17), (16.5, 7), (5, 7), (-2, 6.5), (-2, 1), (-10, 0), (-18, -2)]
    arrow(d, ring, B, 6)
    tag(d, P(-17.6, 6), "① 地面大环\n(绕掩体岛一圈)", fg=(30, 70, 180))
    # level-2 loop (purple)
    Pp = (150, 60, 190)
    loop2 = [(-0.5, 5.5), (0.8, 3.2), (9.7, 3.2), (9.7, 0.2), (3, -1), (3, -5.5), (12, -5.5), (12, 0.2), (9.7, 3.2), (17.0, 3.2), (16.5, 7)]
    arrow(d, loop2, Pp, 6)
    tag(d, P(1.0, -6.3), "② 二层回环:上坡 → 床顶绕一圈 → 下坡", fg=(120, 40, 160))
    tag(d, P(0.4, 5.9), "上坡", fg=(120, 40, 160)); tag(d, P(13.6, 5.7), "下坡", fg=(120, 40, 160))
    # trampoline one-way
    px, py = P(*TRAMP); d.ellipse([px - 9, py - 9, px + 9, py + 9], fill=(255, 150, 60), outline=(150, 70, 0), width=2)
    arrow(d, [(-3.5, -2), (1.5, -2.5)], (255, 140, 40), 4); tag(d, (px - 20, py + 12), "弹床:只上不下", fg=(170, 80, 0))
    cannon(d, *CANNON); tag(d, P(13.2, -3.6), "大炮", fg=(200, 30, 30))
    player(d, *SPAWN)
    # width annotations
    def dim(a, b, text):
        arrow(d, [a, b], (90, 90, 90), 2, False)
        tag(d, P((a[0] + b[0]) / 2 - 1.2, (a[1] + b[1]) / 2 + 0.9), text, fg=(70, 70, 70))
    dim((-4.9, 8.0), (0.0, 8.0), "4.9m"); dim((-21, -0.5), (-14.6, -0.5), "6.4m"); dim((12.8, 12), (18, 12), "5.2m")
    # no dead-end markers
    for k, pts in CH.items():
        cx = sum(p[0] for p in pts) / 3; cz = sum(p[1] for p in pts) / 3
        px, py = P(cx, cz); d.text((px, py - 18), "无死角", font=fs, fill=(255, 255, 255), anchor="mm")
    airwall(d)
    panel(im, "3 · 运动流线", "A3 · 两个环,一个连续回环",
          [("流线", ["蓝 ① 地面大环:绕掩体岛一圈,任何位置都有两个方向可跑。",
                    "紫 ② 二层回环:地面 → 上坡 → 床顶绕大炮一圈 → 下坡 → 回地面,首尾相连。",
                    "两个环在两个坡脚处相交,上下层是连续的一整圈。",
                    "绿:野羊从 4 个玩具箱涌入中央。"]),
           ("无死角检查", ["白字“无死角”= 凹角已用斜面封堵(西北、东北、西南、床西侧、床东侧由坡脚填满)。",
                         "掩体岛四周通道 ≥ 4.9m,没有窄缝。",
                         "床顶西、北两边是开放边缘,可以跳下,不会被卡住。"]),
           ("要测", ["放大 2 倍后的主播(约 1.4m)上人字坡的坡度是否顺手。", "床顶 8.6m 宽 ,50 只羊会不会太挤。"])])
    im.save(os.path.join(OUT, "A3_3_Flow.png"))

img1(); img2(); img3()
print("done")
