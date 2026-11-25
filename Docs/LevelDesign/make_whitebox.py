# -*- coding: utf-8 -*-
# Top-down whitebox schematics for SheepArena, drawn from real asset footprints in the Bedroom scene.
from PIL import Image, ImageDraw, ImageFont
import os

OUT = os.path.dirname(os.path.abspath(__file__))
S = 20                      # px per metre
X0, X1, Z0, Z1 = -27.0, 23.0, -23.0, 27.0
MAPW, MAPH = int((X1 - X0) * S), int((Z1 - Z0) * S)
PANEL = 520
FONT = "C:/Windows/Fonts/msyh.ttc"
f_s = ImageFont.truetype(FONT, 13)
f_m = ImageFont.truetype(FONT, 16)
f_l = ImageFont.truetype(FONT, 24)
f_xl = ImageFont.truetype(FONT, 30)

def P(x, z):
    return ((x - X0) * S, (Z1 - z) * S)

# (name, x0, z0, x1, z1, top)
FURN = [
    ("床 Bed_Grp", -0.2, -7.1, 18.0, 13.1, 5.6),
    ("书桌A", -1.6, -18.5, 5.0, -13.3, 5.5),
    ("书桌B", -12.5, -18.6, -4.7, -11.8, 5.5),
    ("衣柜A", -25.0, -15.2, -19.4, -7.5, 14.6),
    ("衣柜B", 5.9, -21.4, 13.6, -15.7, 14.6),
    ("床头柜", 14.8, -12.6, 17.7, -9.0, 3.0),
    ("椅子", -23.9, 4.2, -20.3, 8.1, 6.6),
]
CLUTTER = [  # floor / low toy clusters from Objects_Grp_*
    (-3.8, 9.2, 4.8, 16.8, 2.4), (3.5, 5.5, 5.4, 11.3, 1.4), (7.5, 2.4, 13.4, 13.2, 4.6),
    (-5.0, 12.7, -2.0, 16.0, 2.7), (-9.5, 12.4, -6.4, 14.3, 0.7), (-19.1, 6.2, -12.2, 14.7, 1.4),
    (-10.8, 15.4, -7.5, 21.6, 1.2), (-14.6, 3.8, -10.3, 11.3, 2.3), (-7.5, 16.5, -2.0, 21.7, 3.5),
    (-11.4, 9.2, -4.9, 12.7, 5.5), (-10.4, -12.3, -2.5, -1.1, 3.0), (-17.0, -3.2, -7.8, 4.9, 2.6),
    (-20.7, -7.4, -16.0, 6.2, 0.8), (-21.6, 0.9, -19.1, 9.1, 1.6), (5.5, 13.3, 9.3, 17.2, 4.0),
    (-14.4, 13.9, -10.9, 17.5, 4.0), (2.5, 16.3, 8.2, 23.1, 0.2),
]
ROAD = (-18.6, 4.5, -6.1, 17.0)   # Road_Sign_Gr toy road area (decor, walkable)
RAMP = (5.7, 2.6, 7.6, 10.7)      # Road_03_Bend_03 : ramp up to bed
PADS = [("弹床", 6.5, 4.35), ("转盘弹射", -18.6, 0.1), ("弹床(桌面)", -1.5, -16.4)]

def shade(top):
    if top >= 10: return (150, 150, 158)
    if top >= 4:  return (178, 170, 150)
    if top >= 2:  return (200, 196, 182)
    return (218, 214, 204)

def base():
    W, H = MAPW + PANEL, MAPH
    im = Image.new("RGB", (W, H), (245, 243, 238))
    d = ImageDraw.Draw(im, "RGBA")
    # floor
    a, b = P(-26, 26); c, e = P(22, -22)
    d.rectangle([a, b, c, e], fill=(236, 230, 214), outline=(90, 90, 90), width=2)
    for gx in range(-26, 23, 8):
        d.line([P(gx, -22), P(gx, 26)], fill=(220, 213, 196), width=1)
    for gz in range(-22, 27, 8):
        d.line([P(-26, gz), P(22, gz)], fill=(220, 213, 196), width=1)
    # toy road
    r = ROAD
    d.rectangle([P(r[0], r[3]), P(r[2], r[1])], fill=(250, 232, 190, 140))
    for (x0, z0, x1, z1, top) in CLUTTER:
        d.rectangle([P(x0, z1), P(x1, z0)], fill=shade(top), outline=(140, 135, 120))
    for (n, x0, z0, x1, z1, top) in FURN:
        d.rectangle([P(x0, z1), P(x1, z0)], fill=shade(top), outline=(90, 85, 75), width=2)
        cx, cy = P((x0 + x1) / 2, (z0 + z1) / 2)
        d.text((cx, cy), n, font=f_s, fill=(60, 55, 50), anchor="mm")
    d.rectangle([P(RAMP[0], RAMP[3]), P(RAMP[2], RAMP[1])], fill=(120, 190, 120, 200), outline=(40, 120, 40))
    d.text(P(6.65, 6.6), "坡道", font=f_s, fill=(20, 80, 20), anchor="mm")
    for (n, x, z) in PADS:
        px, py = P(x, z)
        d.ellipse([px - 7, py - 7, px + 7, py + 7], fill=(255, 150, 60), outline=(150, 70, 0))
    # perimeter air walls (just inside the room walls)
    a, b = P(-25.13, 25.13); c, e = P(21.13, -21.13)
    d.rectangle([a, b, c, e], outline=(220, 30, 30), width=4)
    d.text((a + 6, b - 20), "空气墙 AirWall(四周+天花板) layer 8", font=f_s, fill=(200, 30, 30))
    return im, d

def wall(d, pts, label=None, w=7, col=(200, 40, 40)):
    d.line([P(*p) for p in pts], fill=col, width=w)
    if label:
        d.text(P(*pts[0]), label, font=f_s, fill=col)

def gate(d, x, z, label="口"):
    px, py = P(x, z)
    d.ellipse([px - 9, py - 9, px + 9, py + 9], fill=(60, 200, 90), outline=(20, 100, 40), width=2)

def arrow(d, pts, col=(40, 90, 220), w=5):
    d.line([P(*p) for p in pts], fill=col, width=w, joint="curve")
    (x1, z1), (x2, z2) = pts[-2], pts[-1]
    ex, ey = P(x2, z2); sx, sy = P(x1, z1)
    import math
    ang = math.atan2(ey - sy, ex - sx)
    for da in (2.6, -2.6):
        d.line([(ex, ey), (ex + 16 * math.cos(ang + da), ey + 16 * math.sin(ang + da))], fill=col, width=w)

def marker(d, x, z, text, kind):
    px, py = P(x, z)
    if kind == "spawn":    # player
        d.ellipse([px - 13, py - 13, px + 13, py + 13], fill=(40, 110, 240), outline="white", width=3)
        d.text((px, py), "主", font=f_m, fill="white", anchor="mm")
    elif kind == "box":    # toy box (sheep spawn)
        d.rectangle([px - 13, py - 13, px + 13, py + 13], fill=(150, 95, 50), outline=(70, 40, 10), width=3)
        d.text((px, py), "箱", font=f_m, fill="white", anchor="mm")
    elif kind == "counter":
        d.polygon([(px, py - 16), (px + 15, py + 12), (px - 15, py + 12)], fill=(230, 60, 60), outline="white")
        d.text((px, py + 3), "炮", font=f_s, fill="white", anchor="mm")
    elif kind == "pool":
        d.ellipse([px - 16, py - 16, px + 16, py + 16], fill=(80, 170, 240), outline="white", width=3)
        d.text((px, py), "水", font=f_m, fill="white", anchor="mm")
    if text:
        d.text((px + 18, py - 10), text, font=f_s, fill=(30, 30, 30))

def tag(d, xy, text, fg=(40, 40, 40), font=None):
    font = font or f_s
    b = d.multiline_textbbox(xy, text, font=font)
    d.rectangle([b[0] - 3, b[1] - 2, b[2] + 3, b[3] + 2], fill=(255, 255, 255, 215))
    d.multiline_text(xy, text, font=font, fill=fg)

def region(d, box, col, label):
    d.rectangle([P(box[0], box[3]), P(box[2], box[1])], fill=col + (55,), outline=col + (200,), width=3)
    if label: tag(d, P(box[0] + 0.6, box[3] - 0.4), label, fg=col, font=f_m)

def panel(im, d, title, sub, lines, assets, risks):
    x = MAPW + 20
    y = 18
    d.text((x, y), title, font=f_xl, fill=(30, 30, 30)); y += 44
    d.text((x, y), sub, font=f_m, fill=(90, 90, 90)); y += 34
    for head, items in (("布局要点", lines), ("复用的现有资产", assets), ("风险 / 要测的点", risks)):
        d.text((x, y), head, font=f_l, fill=(30, 60, 150)); y += 34
        for it in items:
            # simple wrap
            cur = ""
            for ch in it:
                if d.textlength(cur + ch, font=f_s) > PANEL - 40:
                    d.text((x, y), cur, font=f_s, fill=(40, 40, 40)); y += 20; cur = ch
                else:
                    cur += ch
            d.text((x, y), cur, font=f_s, fill=(40, 40, 40)); y += 22
        y += 10
    # legend
    y = MAPH - 215
    d.text((x, y), "图例", font=f_l, fill=(30, 60, 150)); y += 34
    leg = [("主", "主播出生点", "spawn"), ("箱", "玩具箱(野羊出口)", "box"), ("炮", "反击点:大炮", "counter"), ("水", "反击点:水池", "pool")]
    for (_, t, k) in leg:
        marker(d, X0 + (x - 0) / S + 1.0 - 0.0, 0, "", k) if False else None
    yy = y
    items = [("spawn", "主播出生点"), ("box", "玩具箱 = 野羊出口"), ("counter", "反击点(大炮)"), ("pool", "反击点(水池)")]
    for k, t in items:
        px = x + 14
        if k == "spawn": d.ellipse([px - 11, yy - 11 + 10, px + 11, yy + 11 + 10], fill=(40, 110, 240))
        if k == "box": d.rectangle([px - 11, yy - 1, px + 11, yy + 21], fill=(150, 95, 50))
        if k == "counter": d.polygon([(px, yy - 2), (px + 12, yy + 22), (px - 12, yy + 22)], fill=(230, 60, 60))
        if k == "pool": d.ellipse([px - 12, yy - 2, px + 12, yy + 22], fill=(80, 170, 240))
        d.text((px + 24, yy + 10), t, font=f_s, fill=(40, 40, 40), anchor="lm"); yy += 30
    px = x + 220; yy = y
    d.line([(px - 14, yy + 10), (px + 14, yy + 10)], fill=(200, 40, 40), width=7); d.text((px + 24, yy + 10), "新增内墙(用书/纸箱摆)", font=f_s, fill=(40, 40, 40), anchor="lm"); yy += 30
    d.ellipse([px - 9, yy + 1, px + 9, yy + 19], fill=(60, 200, 90), outline=(20, 100, 40), width=2); d.text((px + 24, yy + 10), "出入口/缺口", font=f_s, fill=(40, 40, 40), anchor="lm"); yy += 30
    d.line([(px - 14, yy + 10), (px + 14, yy + 10)], fill=(40, 90, 220), width=5); d.text((px + 24, yy + 10), "推荐绕圈路线", font=f_s, fill=(40, 40, 40), anchor="lm"); yy += 30
    d.ellipse([px - 7, yy + 3, px + 7, yy + 17], fill=(255, 150, 60)); d.text((px + 24, yy + 10), "现有弹床/弹射盘", font=f_s, fill=(40, 40, 40), anchor="lm")
    d.text((x, MAPH - 22), "示意图:按现有资产占地绘制,具体坐标进场景后再微调", font=f_s, fill=(130, 130, 130))
    d.text((12, MAPH - 22), "N ↑   1 格 = 8m", font=f_s, fill=(110, 110, 110))

# ---------------------------------------------------------------- Scheme A
def scheme_a():
    im, d = base()
    region(d, (-0.2, -7.1, 18.0, 13.1), (200, 120, 0), "床顶高地(5.6m)")
    marker(d, 9, 8, "反击点:床顶大炮", "counter")
    # ring lane
    arrow(d, [(-4, -9.5), (-4, 19), (14, 21), (19.6, 8), (19.6, -14), (9, -10.5), (-4, -9.5)])
    gate(d, 19.6, 5, "")
    d.text(P(15.2, 5.0), "东侧窄口 3.1m\n(故意做的咽喉)", font=f_s, fill=(20, 100, 40))
    marker(d, -6, 1.5, "主播出生", "spawn")
    marker(d, 14, 22, "", "box"); marker(d, -21, 22, "", "box"); marker(d, -22.5, -19, "", "box"); marker(d, 17.5, -17.5, "", "box")
    d.text(P(-12, -4.5), "西侧通道(很宽)\n清掉部分散件", font=f_s, fill=(60, 60, 60))
    d.text(P(-8, 24), "北侧大广场:野羊主战场", font=f_m, fill=(60, 60, 60))
    panel(im, d, "方案 A · 绕床环道", "整间卧室都用,床是中心岛",
          ["床(18×20m)当巨型中心障碍,四周围成一圈环形跑道,永远有路绕,主播不会被逼进死角。",
           "床顶 5.6m 高地放反击点(大炮),只能走坡道/弹床上去,上去有风险有回报。",
           "东侧床与墙之间只有 3m 窄口,是自然咽喉:负极时羊群在这里挤成一串,可以用来制造\"险\"。",
           "4 个玩具箱放在 4 个角落,野羊从房间四角涌向中间,看起来像包围。"],
          ["Bed_Grp 整块(中心岛)", "Road_03_Bend_03 坡道 + 弹床(上床)", "Cardboard_Box_01/02 当玩具箱", "桌子/衣柜/床头柜 当南侧外圈障碍", "Road_Sign_Gr 玩具路 当西侧通道地面"],
          ["环太长(约 100m),羊追不上、主播也追不到羊,要加速或缩短环。", "床顶高地 NavMesh 要和坡道连通,否则羊上不去。", "竖屏时相机很难同时看到整圈,更适合横屏。"])
    im.save(os.path.join(OUT, "SheepArena_Whitebox_A_BedRing.png"))

# ---------------------------------------------------------------- Scheme B
def scheme_b():
    im, d = base()
    region(d, (-25, 0, -9.5, 25), (60, 150, 60), "区① 玩具场")
    region(d, (-19, -19, 6, -11), (150, 80, 180), "区② 书桌高台(5.5m)")
    region(d, (1, 13.5, 21, 25), (200, 120, 0), "区③ 床北广场")
    region(d, (-9.5, -8, 0, 12), (90, 90, 90), "中央枢纽")
    wall(d, [(-9.5, 25), (-9.5, 19)]); wall(d, [(-9.5, 14), (-9.5, 3)])
    gate(d, -9.5, 16.5)
    wall(d, [(-25, -8.5), (-14, -8.5)]); wall(d, [(-9, -8.5), (0, -8.5)])
    gate(d, -11.5, -8.5)
    wall(d, [(1, 13.5), (1, 20)]); gate(d, 1, 22.5)
    marker(d, -5, 3, "反击点:水池", "pool")
    marker(d, -5, 3, "", "pool")
    marker(d, -2, -3, "主播出生", "spawn")
    marker(d, -21, 22, "", "box"); marker(d, 12, 21, "", "box"); marker(d, -22.5, -4, "", "box")
    arrow(d, [(-5, 6), (-12, 16.5), (-18, 20), (-18, 8), (-11.5, -4), (-5, 3)])
    arrow(d, [(-3, 6), (3, 22.5), (14, 22), (14, 15)], col=(40, 90, 220))
    d.text(P(-24, -4), "桌椅阶梯:椅子/凳子\n当上高台的台阶", font=f_s, fill=(100, 40, 130))
    panel(im, d, "方案 B · 三区 + 中央枢纽", "整间卧室都用,用书墙隔成三个主题区",
          ["三个主题区各有性格:玩具场(开阔)、书桌高台(垂直、安全屋)、床北广场(大场地)。",
           "中央枢纽是十字路口,水池反击点放这里,每个区都两步能到,主播可以随时冲进去反击。",
           "每个区 2 个出入口,没有单口死胡同;区与区之间用书堆/纸箱墙(红线)隔开。",
           "书桌高台要靠椅子/凳子/弹床上去,负极时主播可以爬上去躲,但羊能追上去。"],
          ["Table_01_Grp ×2 + Chair/Stool 书桌区", "Book_Pile / Cardboard_Box / Modular_Cube_Pile 当内墙", "Objects_Grp_05/07/15 玩具场障碍岛", "Road_Sign_Gr 玩具路 当区①主路", "现有弹床 / 转盘弹射 当区间捷径"],
          ["内墙多,NavMesh 要重烘,门口宽度要比放大后的主播(2倍)更宽。", "三区风格差异大,美术成本最高。", "中央枢纽可能太开阔,负极时羊从四面八方来。"])
    im.save(os.path.join(OUT, "SheepArena_Whitebox_B_ThreeZones.png"))

# ---------------------------------------------------------------- Scheme C
def scheme_c():
    im, d = base()
    # darken out-of-arena
    d.rectangle([P(-5, 26), P(22, -22)], fill=(60, 60, 60, 120))
    d.rectangle([P(-26, -6), P(-5, -22)], fill=(60, 60, 60, 120))
    d.text(P(2, 2), "不使用区域\n(用内墙封死)", font=f_l, fill=(255, 255, 255))
    region(d, (-25, -6, -5, 25), (30, 140, 200), "竖屏擂台 20×31m")
    wall(d, [(-5, 25), (-5, -6)], None, w=9); wall(d, [(-25, -6), (-5, -6)], None, w=9)
    d.text(P(-4.6, 10), "书堆/纸箱封墙", font=f_s, fill=(200, 40, 40))
    marker(d, -23, -3, "主播出生", "spawn")
    marker(d, -22, 23, "", "box"); marker(d, -8, 23.5, "", "box"); marker(d, -23, 12, "", "box")
    marker(d, -17, 20, "反击点:水池", "pool")
    marker(d, -8, 0, "反击点:大炮", "counter")
    arrow(d, [(-22, -3), (-22, 16), (-14, 20), (-8, 8), (-8, -3), (-14, -4), (-22, -3)])
    gate(d, -9.5, 15)
    d.text(P(-24.5, -4.6), "障碍岛 = 现有 Objects_Grp_05/07/11/15/16", font=f_s, fill=(60, 60, 60))
    panel(im, d, "方案 C · 竖屏擂台", "只用房间西北约 40%,配竖屏直播",
          ["只留 20×31m(接近 9:14)的西北区域,用内墙封死其余部分,适合 TikTok 竖屏直播。",
           "不用大动场景:房间里本来就有一大堆玩具堆,直接当擂台里的障碍岛,四个岛之间留绕圈。",
           "场地小,羊密度高、冲突快,负极更紧张;水池放北面,大炮放南面,主播在两端之间折返。",
           "相机固定俯视,一屏看全,观众中途进场 3 秒内能看懂。"],
          ["Objects_Grp_05/07/11/15/16 障碍岛", "Board_03_Long / Skate_Grp 当内墙", "Cardboard_Box 当玩具箱", "Road_Sign_Gr 当擂台地面", "转盘弹射 当擂台内弹射点"],
          ["场地小,50 只羊 + 放大的主播可能太挤,要测羊的体型。", "放弃了床和书桌,场景不够有名气。", "封墙长度约 50m,要用资产补齐,工作量不小。"])
    im.save(os.path.join(OUT, "SheepArena_Whitebox_C_PortraitArena.png"))


# ---------------------------------------------------------------- Scheme A2
def scheme_a2():
    import random
    global FURN
    FURN = [(n, x0, z0, x1, z1, t) if n != "床 Bed_Grp" else ("床(二层大平台)", -0.2, -7.1, 18.0, 1.5, 3.6)
            for (n, x0, z0, x1, z1, t) in FURN]
    im, d = base()
    cleared = [(-3.8, 9.2, 4.8, 16.8), (3.5, 5.5, 5.4, 11.3), (-5.0, 12.7, -2.0, 16.0), (-9.5, 12.4, -6.4, 14.3),
               (-10.8, 15.4, -7.5, 21.6), (-14.6, 3.8, -10.3, 11.3), (-7.5, 16.5, -2.0, 21.7),
               (-11.4, 9.2, -4.9, 12.7), (-10.4, -12.3, -2.5, -1.1), (-17.0, -3.2, -7.8, 4.9)]
    for (x0, z0, x1, z1) in cleared:
        d.rectangle([P(x0, z1), P(x1, z0)], fill=(236, 230, 214))
        d.rectangle([P(x0, z1), P(x1, z0)], outline=(150, 150, 150), width=1)
    # main battle zone
    region(d, (-14, 2, 18, 25), (30, 140, 200), "主战区(开阔)")
    region(d, (-14, -10, -1, 2), (30, 140, 200), "")
    # scattered toys
    keep = [(5.5, 13.3, 9.3, 17.2), (-14.4, 13.9, -10.9, 17.5), (2.5, 16.3, 8.2, 23.1), (7.5, 2.4, 13.4, 13.2),
            (5.7, 2.6, 7.6, 10.7), (-0.2, -7.1, 18.0, 1.5)]
    rnd = random.Random(11)
    cols = [(230, 90, 90), (90, 160, 230), (240, 190, 60), (110, 200, 120), (190, 110, 220)]
    placed = 0
    while placed < 46:
        x = rnd.uniform(-13.5, 17.5); z = rnd.uniform(-9.5, 24.5)
        if z < 2 and x > -1: continue
        if z > 22.8 or (z > 19 and x < -9) : continue
        if any(a - 0.8 < x < c + 0.8 and b - 0.8 < z < e + 0.8 for (a, b, c, e) in keep): continue
        w = rnd.uniform(0.7, 1.4)
        d.rectangle([P(x - w / 2, z + w / 2), P(x + w / 2, z - w / 2)], fill=rnd.choice(cols), outline=(70, 70, 70))
        placed += 1
    tag(d, P(-9, 24.2), "散落的小玩具:把现有 PF_Cube_Toys / PF_Pen 物理小件打散", fg=(20, 80, 130))
    d.text(P(-13.6, -9.4), "已清空的大件堆(虚线格):\n挪到房间四周当掩体", font=f_s, fill=(110, 110, 110))
    # second level
    region(d, (0, -7.1, 18, 1.5), (200, 120, 0), "二层 · 床顶大平台 18×8.6m")
    region(d, (-12.5, -18.6, -4.7, -11.8), (150, 80, 180), "")
    region(d, (-1.6, -18.5, 5.0, -13.3), (150, 80, 180), "")
    d.text(P(-12.2, -12.4), "书桌B", font=f_s, fill=(100, 40, 130))
    d.text(P(-1.3, -13.9), "书桌A", font=f_s, fill=(100, 40, 130))
    # existing / new connections
    d.line([P(4.9, -4.3), P(4.9, -14.0)], fill=(150, 80, 180), width=10)
    d.text(P(5.2, -9.5), "既有木板天桥\n(书桌A↔床)", font=f_s, fill=(100, 40, 130))
    d.line([P(-1.1, -17.1), P(-7.9, -17.1)], fill=(150, 80, 180), width=10)
    tag(d, P(-9.5, -20.6), "既有尺子桥(书桌A↔书桌B)", fg=(100, 40, 130))
    # access points up to level 2
    def up(x, z, n, txt, dx=16, dy=-10):
        px, py = P(x, z)
        d.polygon([(px, py - 15), (px + 14, py + 11), (px - 14, py + 11)], fill=(40, 170, 80), outline="white")
        d.text((px, py + 3), n, font=f_s, fill="white", anchor="mm")
        tag(d, (px + dx, py + dy), txt, fg=(20, 90, 40))
    up(6.6, 5.0, "1", "既有坡道(北面上床)", dx=-150, dy=-30)
    up(10.5, 3.2, "2", "既有弹床 → 直接弹上床", dx=20, dy=-26)
    up(-3.0, -3.5, "3", "新增坡:复制现有木板", dx=-170, dy=-34)
    up(-14.2, -8.6, "4", "椅子/凳子阶梯→书桌B", dx=-10, dy=-52)
    # ground loop around bed + level-2 loop
    arrow(d, [(-8, -4), (-6, 16), (10, 21), (20.2, 12), (20.2, -10), (9, -10.8), (-2, -10.8), (-8, -4)], col=(40, 90, 220))
    arrow(d, [(12, -3), (4.9, -5.5), (4.9, -12.5), (-1, -16.3), (-8, -16.3), (-12.5, -11.5), (-12.5, -8), (-8, -5.5), (-3.5, -3.5), (6, -3.5)], col=(150, 80, 180), w=4)
    marker(d, 13, -2.8, "", "counter"); tag(d, P(10.0, -5.2), "反击点:床上大炮", fg=(200, 30, 30))
    marker(d, -6, 7, "主播出生", "spawn")
    marker(d, 17, 22.5, "", "box"); marker(d, -21, 22.5, "", "box"); marker(d, -23.5, 10, "", "box"); marker(d, 17.5, -17.5, "", "box")
    panel(im, d, "方案 A2 · 开阔中央战区 + 二层平台", "在 A 的环线上改:中间清空做主战区,床和书桌连成二层",
          ["主战区 ≈ 900m²:房间中部和北部全部清空,只留零散小玩具和几堆掩体,是整张图最大最开阔的地方。",
           "二层 ≈ 240m²:床顶(18×8.6m)是次级大平台;两张书桌和床靠现有木板、尺子连成一圈。",
           "4 条上二层的通道(绿三角):既有坡道、弹床、新增中央坡、椅凳阶梯,不用的羊也能追上去。",
           "反击点大炮放在床上:主播冲上二层有收益,也有风险(负极时被追上去会无路可退)。",
           "A 的两个环都保留:地面绕床一圈(蓝),二层绕桌绕床一圈(紫);四角玩具箱出羊。"],
          ["Bed_Grp(床顶当次级平台)", "Board_03_Long(既有木板天桥,可复制出新坡)", "Mesure_Long(尺子桥)", "Road_03_Bend_03 坡道 + 弹床", "Chair / Stool 阶梯", "Cardboard_Box 当玩具箱/掩体", "PF_Cube_Toys / PF_Pen 当散落玩具"],
          ["床实际只有 18×8.6m、约 3.6m 高(之前图里画成 18×20、5.6m,画错了,这版已按实际尺寸改正)。", "既有坡道能否真的走上床顶,要进场景试走确认,不通的话改成新增坡。", "放大 2 倍后的主播(1.4m 宽)通过木板天桥和尺子桥是否太窄,要测。"])
    im.save(os.path.join(OUT, "SheepArena_Whitebox_A2_OpenCenter.png"))

import sys
if 'a2' in sys.argv:
    scheme_a2()
else:
    scheme_a(); scheme_b(); scheme_c()
print("done")
