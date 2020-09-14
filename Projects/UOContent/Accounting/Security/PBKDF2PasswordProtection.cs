/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PBKDF2PasswordProtection.cs                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Server.Misc;

namespace Server.Accounting.Security
{
    public class PBKDF2PasswordProtection : IPasswordProtection
    {
        private const ushort m_MinIterations = 1024;
        private const ushort m_MaxIterations = 1536;
        private const int m_SaltSize = 8;
        private const int m_HashSize = 32;
        private const int m_OutputSize = 2 + m_SaltSize + m_HashSize;
        public static IPasswordProtection Instance = new PBKDF2PasswordProtection();
        private static readonly HashAlgorithmName m_Algorithm = HashAlgorithmName.SHA256;

        public string EncryptPassword(string plainPassword)
        {
            Span<byte> output = stackalloc byte[m_OutputSize];
            var iterations = Utility.RandomMinMax(m_MinIterations, m_MaxIterations);
            BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(0, 2), (ushort)iterations);

            var rfc2898 = new Rfc2898DeriveBytes(plainPassword, m_SaltSize, iterations, m_Algorithm);
            rfc2898.Salt.CopyTo(output.Slice(2, m_SaltSize));
            rfc2898.GetBytes(m_HashSize).CopyTo(output.Slice(m_SaltSize + 2));

            return HexStringConverter.GetString(output);
        }

        public bool ValidatePassword(string encryptedPassword, string plainPassword)
        {
            Span<byte> encryptedBytes = stackalloc byte[m_OutputSize];
            HexStringConverter.GetBytes(encryptedPassword, encryptedBytes);

            var iterations = BinaryPrimitives.ReadUInt16LittleEndian(encryptedBytes.Slice(0, 2));
            var salt = encryptedBytes.Slice(2, m_SaltSize);

            ReadOnlySpan<byte> hash =
                new Rfc2898DeriveBytes(plainPassword, salt.ToArray(), iterations, m_Algorithm).GetBytes(m_HashSize);

            return hash.SequenceEqual(encryptedBytes.Slice(m_SaltSize + 2));
        }
    }
}
