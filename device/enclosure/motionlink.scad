// MotionLink HW-02 prototype. Dimensions in mm. Each part is oriented for printing.
part = "tray"; // [tray,lid,gauges,mount]

// Grip and replaceable mount
plug_major = 3.1;
plug_minor = 2.65;
plug_length = 4;
plug_pitch = 1.2;
seat_diameter = 20;
seat_dish = 0.9;
mount_center = [0, -1];

// The USB end is +Y. The switch is on +X. The battery sits below the PCB.
left = -15;
right = 19;
back = -16.5;
front = 14;
wall = 1.6;
floor_height = 3.6;
rim = 14.4;
lid_height = 1.8;
corner = 3;

usb_x = 0;
usb_width = 13.5;
usb_bottom = 9.1;
switch_y = 4.8;
switch_shelf = 8.7;
switch_pocket_length = 9.3;
switch_wall = 1.2;

$fn = 48;
eps = 0.02;
boss_centers = [
    [left + 2.4, back + 2.4], [right - 2.4, back + 2.4],
    [left + 2.4, front - 2.4], [right - 2.4, front - 2.4]
];

module outline(inset = 0) {
    hull()
        for (x = [left + corner, right - corner],
             y = [back + corner, front - corner])
            translate([x, y]) circle(r = corner - inset);
}

module screw_holes(diameter, bottom, height) {
    for (p = boss_centers)
        translate([p[0], p[1], bottom])
            cylinder(d = diameter, h = height);
}

module switch_cradle() {
    cradle_x = right - switch_wall - 3.7;
    cradle_y = switch_y - switch_pocket_length / 2;
    translate([cradle_x, cradle_y, floor_height - eps])
        cube([right - cradle_x - 0.3, switch_pocket_length,
              switch_shelf - floor_height + eps]);
    for (y = [cradle_y - 1.2, cradle_y + switch_pocket_length])
        translate([cradle_x, y, floor_height - eps])
            cube([right - cradle_x - 0.3, 1.2, rim - floor_height + eps]);
}

module tray() {
    difference() {
        union() {
            difference() {
                linear_extrude(rim) outline();
                translate([0, 0, floor_height])
                    linear_extrude(rim) outline(wall);
            }
            for (p = boss_centers)
                translate([p[0], p[1], floor_height - eps])
                    cylinder(d = 4.4, h = rim - floor_height + eps);
            switch_cradle();
        }
        screw_holes(1.65, rim - 5.2, 5.2 + eps);
        // Open-top notches let the prewired assembly drop in from above.
        translate([usb_x - usb_width / 2, front - wall - eps, usb_bottom])
            cube([usb_width, wall + 2 * eps, rim - usb_bottom + eps]);
        // Recess the inner wall to place the switch face close to its actuator opening.
        translate([right - wall - eps, switch_y - switch_pocket_length / 2, switch_shelf])
            cube([wall - switch_wall + eps, switch_pocket_length, rim - switch_shelf + eps]);
        translate([right - switch_wall - eps, switch_y - 2.75, switch_shelf + 0.5])
            cube([switch_wall + 2 * eps, 5.5, rim - switch_shelf - 0.5 + eps]);
        translate([mount_center[0], mount_center[1], -eps]) {
            cylinder(d = 3.3, h = floor_height + 2 * eps);
            translate([0, 0, 1.8 + eps])
                cylinder(d = 6.6, h = floor_height - 1.8 + eps);
        }
    }
}

module lid() {
    // The visible face prints on the bed. Flip the lid over for assembly.
    difference() {
        union() {
            linear_extrude(lid_height) outline();
            // Capture the metal switch body vertically.
            translate([right - switch_wall - 3.5,
                       switch_y - switch_pocket_length / 2 + 0.35, lid_height - eps])
                cube([3.2, switch_pocket_length - 0.7, rim - switch_shelf - 3.9 + eps]);
        }
        screw_holes(2.25, -eps, lid_height + 2 * eps);
    }
}

module gauge(diameter) {
    union() {
        cylinder(d = 14, h = 2.4);
        translate([0, 0, 2.4 - eps])
            cylinder(d = diameter, h = plug_length - 0.6 + eps);
        translate([0, 0, 2.4 + plug_length - 0.6 - eps])
            cylinder(d1 = diameter, d2 = diameter - 0.8, h = 0.6 + eps);
        translate([0, -3.9, 2.4 - eps])
            linear_extrude(0.42)
                text(str(diameter), size = 2.7, font = "Liberation Sans",
                     halign = "center", valign = "center");
    }
}

// Sample a rounded, single-start thread directly on a tapered core. The
// crest fades at both ends so it starts smoothly and clears the grip face.
module grip_thread() {
    sides = 48;
    slices = ceil(plug_length * 30);
    points = [
        for (i = [0 : slices], j = [0 : sides - 1])
            let(z = plug_length * i / slices,
                angle = 360 * j / sides,
                turns = z / plug_pitch - angle / 360,
                phase = abs(turns - floor(turns + 0.5)),
                crest = (1 + cos(180 * min(phase / 0.4, 1))) / 2,
                fade = min(z / 0.55, (plug_length - z) / 0.85, 1),
                nose = max(0, (z - plug_length + 0.7) / 0.7) * 0.42,
                r = plug_minor / 2 + (plug_major - plug_minor) / 2 * crest * fade - nose)
            [r * cos(angle), r * sin(angle), z]
    ];
    faces = concat(
        [[for (j = [0 : sides - 1]) j]],
        [for (i = [0 : slices - 1], j = [0 : sides - 1])
            let(a = i * sides + j, b = i * sides + (j + 1) % sides,
                c = a + sides, d = b + sides)
            each [[a, c, b], [b, c, d]]],
        [[for (j = [sides - 1 : -1 : 0]) slices * sides + j]]
    );
    polyhedron(points, faces, convexity = 8);
}

module mount() {
    seat_height = 5.6;
    center_height = seat_height - seat_dish;
    union() {
        difference() {
            rotate_extrude()
                polygon([[0, 0], [seat_diameter / 2 - 0.5, 0],
                         [seat_diameter / 2, 0.5], [seat_diameter / 2, seat_height - 0.35],
                         [seat_diameter / 2 - 0.35, seat_height], [0, center_height]]);
            // M3 nut, nominal 5.5 mm across flats, inserted from the case side.
            translate([0, 0, -eps]) cylinder(d = 6.65, h = 2.8 + eps, $fn = 6);
            translate([0, 0, -eps]) cylinder(d = 3.4, h = 4.0 + eps);
        }
        translate([0, 0, center_height - eps]) grip_thread();
    }
}

if (part == "tray") tray();
else if (part == "lid") mirror([0, 1, 0]) lid();
else if (part == "mount") mount();
else if (part == "gauges")
    for (i = [0 : 4]) translate([17 * (i - 2), 0, 0]) gauge(2.6 + 0.2 * i);
