
pub fn lzf2_decompress(input: &[u8]) -> Option<Vec<u8>> {
    if input.is_empty() {
        return None;
    }
    // note: grow factor of Vec should be equal to 2 for better memory efficiency due to contiguous Vec resizing by a factor of 2
    let mut output: Vec<u8> = vec![0; input.len() * 12];

    let mut iidx: usize = 0;
    let mut oidx: usize = 0;

    while iidx < input.len() {
        let mut ctrl: usize = input[iidx] as usize;
        iidx += 1;

        if ctrl < (1 << 5) { // literal run
            ctrl += 1;
            if oidx + ctrl > output.len() {
                output.resize(output.len() * 2, 0);
            }
            if iidx + ctrl > input.len() {
                return None;
            }
            output[oidx..(oidx + ctrl)].copy_from_slice(&input[iidx..(iidx + ctrl)]);
            oidx += ctrl;
            iidx += ctrl;
        } else { // back reference
            let mut len: usize = ctrl >> 5;
            let mut reference = oidx as isize - ((ctrl & 0x1F) << 8) as isize - 1;

            if len == 7 {
                len += *input.get(iidx)? as usize;
                iidx += 1;
            }
            reference -= *input.get(iidx)? as isize;
            iidx += 1;

            if oidx + len + 2 > output.len() {
                output.resize(output.len() * 2, 0);
            }
            if reference < 0 {
                return None;
            }
            let reference = reference as usize;
            // do not use .copy_within since they may overlap
            for i in 0..len + 2 {
                output[oidx + i] = output[reference + i];
            }
            oidx += len + 2;
        }
    }
    output.truncate(oidx);
    return Some(output);
}