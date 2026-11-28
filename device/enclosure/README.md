# MotionLink grip-butt enclosure

HW-02 fit prototype for the Callaway grip shown in the photos. The enclosure is 34 × 30.5 × 16.2 mm before the mount, with an external USB-C opening and a side opening for the prewired slide switch.

## Print the fit parts

1. Open [vent-gauges.stl](vent-gauges.stl) in Creality Print in millimetres. The five separate pins are 2.6, 2.8, 3.0, 3.2 and 3.4 mm nominal diameter. Print as oriented, with the discs flat on the bed.
2. Starting with the smallest, try each gauge by hand on every club. Record the largest gauge that enters without force, how far it goes in, and whether the cap shows any marking. Stop if the pin binds. The 4 mm pin length is a test geometry and does not imply the hole is that deep.
3. If the gauge results are consistent with the estimated 3 mm hole, print [mount-3.1mm.stl](mount-3.1mm.stl). Start it by hand with light pressure and stop if it binds, marks the grip, or fails to seat against the cap. If the 3.1 mm mount seats without marks but pulls out or rotates easily, try [mount-3.3mm.stl](mount-3.3mm.stl) under the same conditions. Each mount has a rounded, shallow thread and a dished seat to clear the raised centre of the grip. If neither works across the clubs, use the gauge results to adjust the CAD before testing again.

Use the Ender-3 V3 Plus PLA profile with the stock 0.4 mm nozzle. Start at 0.12 mm layer height and solid infill for the mounts. Print the mount with the broad face down and the plug up. Leave the nut recess clear of supports. The tray and lid can also print in the supplied orientations, at 0.16–0.20 mm layer height.

## Assemble the draft housing

1. Print [tray.stl](tray.stl) and [lid.stl](lid.stl). Fit an M3 hex nut into the back of the mount, then fasten the mount with an M3 × 5 mm button-head screw from inside the empty tray. Check that the screw head sits below the tray floor and that the screw tightens without bottoming out. Cover the recess with a thin insulating layer before installing the electronics.
2. Drop the taped assembly in with the pouch cell down, USB-C at the broad front notch and the metal switch body on its side shelf. Check wire clearance, switch travel, and that your USB-C cable can seat fully. The board and switch must stay fixed for repeatable IMU readings. If needed, use a thin, removable pad against the PCB shield to take up play. If the lid presses on the LiPo pouch, pinches a wire, or requires force to close, adjust the CAD.
3. Flip the lid over so its raised tab is inside over the switch. Fasten it with four M2 × 6 mm screws into the printed pilot holes, taking care not to strip them.

## Verify HW-02

Check with the empty enclosure first, then with the electronics installed. On each club, check that the case seats without rocking, resists firm hand pull and twist, and leaves no marks after removal. Repeat after several fit/removal cycles. Progress to slow practice swings in a clear area and recheck for rotation, looseness or a cracked plug. Any movement needs a mount revision before full swings. Keep the case aligned consistently relative to the clubface when transferring it between clubs.

The supplied meshes are prototypes. Final fit depends on the measured vent and cap shape, print tolerances, and the actual dimensions of the taped assembly. If a case fit needs adjusting, measure the stack height including solder and wires, the battery envelope with tape, and the switch face location relative to the USB-C connector. [motionlink.scad](motionlink.scad) exposes the mount diameters and length, seat diameter and dish, case bounds, and opening positions. Choose a part in OpenSCAD, render with F6, and export as STL.

The battery and switch dimensions came from the supplier listing and drawing. The positions of the wired components were estimated from the photos. [LiPo](https://thepihut.com/products/150mah-3-7v-lipo-battery-1-25mm-picoblade-connector?variant=55102987567489) · [Switch drawing](https://cdn.shopify.com/s/files/1/0176/3274/files/Datasheet.jpg?v=1692628891)
