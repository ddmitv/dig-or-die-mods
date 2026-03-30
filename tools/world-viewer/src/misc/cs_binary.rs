use anyhow::{Context, Result, ensure};

pub struct CSBinReader<'a> {
    buffer: &'a [u8],
    pos: usize,
}

impl<'a> CSBinReader<'a> {
    pub fn new(buffer: &'a [u8]) -> Self {
        return Self { buffer, pos: 0 };
    }

    pub fn pos(&self) -> usize {
        return self.pos;
    }
    pub fn remaining(&self) -> usize {
        return self.buffer.len() - self.pos;
    }

    fn read_fixed_bytes<const N: usize>(&mut self) -> Result<&'a [u8; N]> {
        let slice = self.buffer[self.pos..]
            .first_chunk::<N>()
            .with_context(|| format!("Unexpected EOF at {:#X} (reading {} bytes)", self.pos, N))?;
        self.pos += N;
        return Ok(slice);
    }

    pub fn read_bytes(&mut self, count: usize) -> Result<&'a [u8]> {
        let slice = self.buffer[self.pos..]
            .get(..count)
            .with_context(|| format!("Unexpected EOF at {:#X} (reading {} bytes)", self.pos, count))?;
        self.pos += count;
        return Ok(slice);
    }

    pub fn skip_bytes(&mut self, count: usize) -> Result<()> {
        ensure!(self.pos + count < self.buffer.len(), "Unexpected EOF at {:#X} (skipping {} bytes)", self.pos, count);
        self.pos += count;
        return Ok(());
    }

    pub fn read_byte(&mut self) -> Result<u8> {
        return Ok(self.read_fixed_bytes::<1>()?[0]);
    }
    pub fn read_sbyte(&mut self) -> Result<i8> {
        return Ok(self.read_fixed_bytes::<1>()?[0] as i8);
    }
    pub fn read_7bit_encoded_int(&mut self) -> Result<i32> {
        let start_pos = self.pos;
        let mut result: u32 = 0;
        for shift in (0..28).step_by(7) {
            let chunk: u32 = self.read_byte()? as u32;
            result |= (chunk & 0x7F) << shift;
            if chunk <= 0x7F {
                return Ok(result as i32);
            }
        }
        let chunk: u32 = self.read_byte()? as u32;
        ensure!(chunk <= 0b1111, "Invalid 7-bit encoding at position {:#X}", start_pos);
        result |= chunk << 28;
        return Ok(result as i32);
    }
    pub fn read_string(&mut self) -> Result<&'a str> {
        let start_pos = self.pos;
        let len = self.read_7bit_encoded_int()?;
        ensure!(len >= 0, "Invalid string length (length: {}) at {:#X}", len, start_pos);
        let bytes = self.read_bytes(len as usize)?;
        return str::from_utf8(bytes).with_context(|| format!("Invalid UTF-8 at {:#X} (string length: {})", start_pos, len));
    }
    pub fn read_bool(&mut self) -> Result<bool> {
        return Ok(self.read_byte()? != 0);
    }
    pub fn read_short(&mut self) -> Result<i16> {
        return Ok(i16::from_le_bytes(*self.read_fixed_bytes::<2>()?));
    }
    pub fn read_ushort(&mut self) -> Result<u16> {
        return Ok(u16::from_le_bytes(*self.read_fixed_bytes::<2>()?));
    }
    pub fn read_int(&mut self) -> Result<i32> {
        return Ok(i32::from_le_bytes(*self.read_fixed_bytes::<4>()?));
    }
    pub fn read_uint(&mut self) -> Result<u32> {
        return Ok(u32::from_le_bytes(*self.read_fixed_bytes::<4>()?));
    }
    pub fn read_long(&mut self) -> Result<i64> {
        return Ok(i64::from_le_bytes(*self.read_fixed_bytes::<8>()?));
    }
    pub fn read_ulong(&mut self) -> Result<u64> {
        return Ok(u64::from_le_bytes(*self.read_fixed_bytes::<8>()?));
    }
    pub fn read_float(&mut self) -> Result<f32> {
        return Ok(f32::from_le_bytes(*self.read_fixed_bytes::<4>()?));
    }
    pub fn read_double(&mut self) -> Result<f64> {
        return Ok(f64::from_le_bytes(*self.read_fixed_bytes::<8>()?));
    }
}
