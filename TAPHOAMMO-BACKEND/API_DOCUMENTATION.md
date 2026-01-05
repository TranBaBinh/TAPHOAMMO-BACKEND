# API Documentation - Authentication & User Management

## Base URL
```
/api/auth
```

## Authentication
Tất cả các endpoints (trừ register, login, request-otp) đều yêu cầu JWT token trong header:
```
Authorization: Bearer <token>
```

---

## 1. Bật/Tắt Xác thực 2 Lớp (2FA)

### Endpoint
```
POST /api/auth/toggle-2fa
```

### Mô tả
Cho phép user bật hoặc tắt xác thực 2 lớp (2FA).

### Headers
```
Authorization: Bearer <token>
Content-Type: application/json
```

### Request Body
```json
{
  "enable": true  // true để bật, false để tắt
}
```

### Response Success (200 OK)
```json
{
  "message": "Đã bật xác thực 2 lớp",  // hoặc "Đã tắt xác thực 2 lớp"
  "twoFactorEnabled": true
}
```

### Response Errors
- **400 Bad Request**: Dữ liệu không hợp lệ
- **401 Unauthorized**: Token không hợp lệ hoặc hết hạn
- **404 Not Found**: User không tồn tại

### Ví dụ sử dụng (JavaScript/Fetch)
```javascript
const toggle2FA = async (enable) => {
  const response = await fetch('/api/auth/toggle-2fa', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ enable })
  });
  
  const data = await response.json();
  return data;
};

// Bật 2FA
await toggle2FA(true);

// Tắt 2FA
await toggle2FA(false);
```

---

## 2. Đổi Mật Khẩu

### Endpoint
```
POST /api/auth/change-password
```

### Mô tả
Cho phép user đổi mật khẩu. Yêu cầu:
- Mật khẩu hiện tại phải đúng
- Mật khẩu mới phải khác mật khẩu cũ
- Mật khẩu mới và xác nhận mật khẩu phải khớp
- **Lưu ý**: User đăng nhập bằng Google không thể đổi mật khẩu

### Headers
```
Authorization: Bearer <token>
Content-Type: application/json
```

### Request Body
```json
{
  "currentPassword": "matkhau123",
  "newPassword": "matkhaumoi456",
  "confirmNewPassword": "matkhaumoi456"
}
```

### Validation Rules
- `currentPassword`: Bắt buộc
- `newPassword`: Bắt buộc, từ 6-100 ký tự
- `confirmNewPassword`: Bắt buộc, phải khớp với `newPassword`

### Response Success (200 OK)
```json
{
  "message": "Đổi mật khẩu thành công"
}
```

### Response Errors
- **400 Bad Request**: 
  - Dữ liệu không hợp lệ
  - Mật khẩu hiện tại không đúng
  - Mật khẩu mới trùng với mật khẩu cũ
  - Tài khoản đăng nhập bằng Google (không thể đổi mật khẩu)
- **401 Unauthorized**: Token không hợp lệ hoặc hết hạn
- **404 Not Found**: User không tồn tại

### Ví dụ sử dụng (JavaScript/Fetch)
```javascript
const changePassword = async (currentPassword, newPassword, confirmPassword) => {
  const response = await fetch('/api/auth/change-password', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      currentPassword,
      newPassword,
      confirmNewPassword: confirmPassword
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Đổi mật khẩu thất bại');
  }
  
  const data = await response.json();
  return data;
};

// Sử dụng
try {
  await changePassword('oldPass123', 'newPass456', 'newPass456');
  alert('Đổi mật khẩu thành công!');
} catch (error) {
  alert(error.message);
}
```

### Lưu ý cho Frontend
- Kiểm tra xem user có đăng nhập bằng Google không (dựa vào `user.googleId` trong profile)
- Nếu user đăng nhập bằng Google, ẩn hoặc disable nút đổi mật khẩu
- Hiển thị thông báo: "Tài khoản đăng nhập bằng Google không thể đổi mật khẩu"

---

## 3. Lấy Thông Tin Ngân Hàng

### Endpoint
```
GET /api/auth/bank-info
```

### Mô tả
Lấy thông tin ngân hàng của user hiện tại.

### Headers
```
Authorization: Bearer <token>
```

### Response Success (200 OK)
```json
{
  "bankName": "Vietcombank",
  "bankAccountNumber": "1234567890",
  "bankAccountHolder": "NGUYEN VAN A",
  "bankBranch": "Chi nhánh Hà Nội"
}
```

### Response khi chưa có thông tin
```json
{
  "bankName": null,
  "bankAccountNumber": null,
  "bankAccountHolder": null,
  "bankBranch": null
}
```

### Response Errors
- **401 Unauthorized**: Token không hợp lệ hoặc hết hạn
- **404 Not Found**: User không tồn tại

### Ví dụ sử dụng (JavaScript/Fetch)
```javascript
const getBankInfo = async () => {
  const response = await fetch('/api/auth/bank-info', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const data = await response.json();
  return data;
};
```

---

## 4. Cập Nhật Thông Tin Ngân Hàng

### Endpoint
```
PUT /api/auth/bank-info
```

### Mô tả
Cập nhật thông tin ngân hàng của user. Có thể cập nhật một hoặc nhiều trường cùng lúc.

### Headers
```
Authorization: Bearer <token>
Content-Type: application/json
```

### Request Body
Tất cả các trường đều tùy chọn (optional):
```json
{
  "bankName": "Vietcombank",              // Tên ngân hàng (tối đa 100 ký tự)
  "bankAccountNumber": "1234567890",      // Số tài khoản (tối đa 50 ký tự)
  "bankAccountHolder": "NGUYEN VAN A",   // Tên chủ tài khoản (tối đa 100 ký tự)
  "bankBranch": "Chi nhánh Hà Nội"        // Chi nhánh (tối đa 200 ký tự)
}
```

### Validation Rules
- `bankName`: Tối đa 100 ký tự
- `bankAccountNumber`: Tối đa 50 ký tự
- `bankAccountHolder`: Tối đa 100 ký tự
- `bankBranch`: Tối đa 200 ký tự

### Response Success (200 OK)
```json
{
  "message": "Cập nhật thông tin ngân hàng thành công",
  "bankInfo": {
    "bankName": "Vietcombank",
    "bankAccountNumber": "1234567890",
    "bankAccountHolder": "NGUYEN VAN A",
    "bankBranch": "Chi nhánh Hà Nội"
  }
}
```

### Response Errors
- **400 Bad Request**: Dữ liệu không hợp lệ (vượt quá độ dài cho phép)
- **401 Unauthorized**: Token không hợp lệ hoặc hết hạn
- **404 Not Found**: User không tồn tại

### Ví dụ sử dụng (JavaScript/Fetch)
```javascript
const updateBankInfo = async (bankInfo) => {
  const response = await fetch('/api/auth/bank-info', {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(bankInfo)
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Cập nhật thất bại');
  }
  
  const data = await response.json();
  return data;
};

// Sử dụng
try {
  await updateBankInfo({
    bankName: 'Vietcombank',
    bankAccountNumber: '1234567890',
    bankAccountHolder: 'NGUYEN VAN A',
    bankBranch: 'Chi nhánh Hà Nội'
  });
  alert('Cập nhật thông tin ngân hàng thành công!');
} catch (error) {
  alert(error.message);
}
```

---

## 5. Lấy Thông Tin Profile (Đã có sẵn - Cập nhật)

### Endpoint
```
GET /api/auth/profile
```

### Mô tả
Lấy thông tin profile đầy đủ của user, bao gồm cả thông tin ngân hàng.

### Headers
```
Authorization: Bearer <token>
```

### Response Success (200 OK)
```json
{
  "id": 1,
  "username": "user123",
  "fullName": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "user",
  "createdAt": "2024-01-01T00:00:00Z",
  "level": 1,
  "totalPurchaseAmount": 0,
  "totalPurchases": 0,
  "totalShops": null,
  "totalSales": null,
  "totalSaleAmount": null,
  "isVerified": false,
  "twoFactorEnabled": false,
  "eKYCFrontImage": null,
  "eKYCBackImage": null,
  "eKYCPortraitImage": null,
  "phone": "0123456789",
  "shopName": null,
  "bankName": "Vietcombank",
  "bankAccountNumber": "1234567890",
  "bankAccountHolder": "NGUYEN VAN A",
  "bankBranch": "Chi nhánh Hà Nội"
}
```

---

## Tổng Hợp Các Endpoints

| Method | Endpoint | Mô tả | Auth Required |
|--------|----------|-------|---------------|
| POST | `/api/auth/toggle-2fa` | Bật/tắt 2FA | ✅ |
| POST | `/api/auth/change-password` | Đổi mật khẩu | ✅ |
| GET | `/api/auth/bank-info` | Lấy thông tin ngân hàng | ✅ |
| PUT | `/api/auth/bank-info` | Cập nhật thông tin ngân hàng | ✅ |
| GET | `/api/auth/profile` | Lấy profile đầy đủ | ✅ |

---

## Lưu Ý Quan Trọng

### 1. Đổi Mật Khẩu
- **User đăng nhập bằng Google** (`googleId` không null và `passwordHash` rỗng) **KHÔNG THỂ** đổi mật khẩu
- Frontend nên kiểm tra và ẩn nút đổi mật khẩu cho user Google
- Validation:
  - Mật khẩu hiện tại phải đúng
  - Mật khẩu mới phải khác mật khẩu cũ
  - Mật khẩu mới và xác nhận phải khớp

### 2. 2FA
- Trạng thái 2FA được lưu trong `User.TwoFactorEnabled`
- Có thể bật/tắt bất cứ lúc nào
- Trạng thái được trả về trong profile

### 3. Thông Tin Ngân Hàng
- Tất cả các trường đều optional
- Có thể cập nhật từng phần (chỉ gửi các trường cần cập nhật)
- Thông tin được lưu trong database và trả về trong profile

### 4. Error Handling
Tất cả các endpoints đều trả về lỗi với format:
```json
{
  "message": "Mô tả lỗi bằng tiếng Việt"
}
```

Frontend nên xử lý các status code:
- `400`: Bad Request - Dữ liệu không hợp lệ
- `401`: Unauthorized - Token không hợp lệ hoặc hết hạn
- `404`: Not Found - Resource không tồn tại
- `500`: Internal Server Error - Lỗi server

---

## Ví Dụ Component React (Tham Khảo)

### Component Đổi Mật Khẩu
```jsx
import { useState } from 'react';

const ChangePasswordForm = ({ user, token }) => {
  const [formData, setFormData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmNewPassword: ''
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Kiểm tra nếu user đăng nhập bằng Google
  if (user?.googleId && !user?.passwordHash) {
    return (
      <div className="alert alert-info">
        Tài khoản đăng nhập bằng Google không thể đổi mật khẩu.
      </div>
    );
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (formData.newPassword !== formData.confirmNewPassword) {
      setError('Mật khẩu nhập lại không khớp');
      return;
    }

    try {
      const response = await fetch('/api/auth/change-password', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          currentPassword: formData.currentPassword,
          newPassword: formData.newPassword,
          confirmNewPassword: formData.confirmNewPassword
        })
      });

      const data = await response.json();

      if (!response.ok) {
        setError(data.message || 'Đổi mật khẩu thất bại');
        return;
      }

      setSuccess(data.message);
      setFormData({
        currentPassword: '',
        newPassword: '',
        confirmNewPassword: ''
      });
    } catch (err) {
      setError('Có lỗi xảy ra. Vui lòng thử lại.');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {error && <div className="alert alert-danger">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      
      <div>
        <label>Mật khẩu hiện tại:</label>
        <input
          type="password"
          value={formData.currentPassword}
          onChange={(e) => setFormData({...formData, currentPassword: e.target.value})}
          required
        />
      </div>
      
      <div>
        <label>Mật khẩu mới:</label>
        <input
          type="password"
          value={formData.newPassword}
          onChange={(e) => setFormData({...formData, newPassword: e.target.value})}
          required
          minLength={6}
        />
      </div>
      
      <div>
        <label>Nhập lại mật khẩu mới:</label>
        <input
          type="password"
          value={formData.confirmNewPassword}
          onChange={(e) => setFormData({...formData, confirmNewPassword: e.target.value})}
          required
        />
      </div>
      
      <button type="submit">Đổi mật khẩu</button>
    </form>
  );
};
```

### Component Bật/Tắt 2FA
```jsx
const Toggle2FA = ({ user, token, onUpdate }) => {
  const [loading, setLoading] = useState(false);

  const handleToggle = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/auth/toggle-2fa', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          enable: !user.twoFactorEnabled
        })
      });

      const data = await response.json();
      
      if (response.ok) {
        onUpdate({ twoFactorEnabled: data.twoFactorEnabled });
      } else {
        alert(data.message || 'Có lỗi xảy ra');
      }
    } catch (err) {
      alert('Có lỗi xảy ra. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <label>
        <input
          type="checkbox"
          checked={user.twoFactorEnabled}
          onChange={handleToggle}
          disabled={loading}
        />
        Xác thực 2 lớp (2FA)
      </label>
    </div>
  );
};
```

### Component Thông Tin Ngân Hàng
```jsx
const BankInfoForm = ({ token, onUpdate }) => {
  const [bankInfo, setBankInfo] = useState({
    bankName: '',
    bankAccountNumber: '',
    bankAccountHolder: '',
    bankBranch: ''
  });
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Load thông tin hiện tại
    fetch('/api/auth/bank-info', {
      headers: { 'Authorization': `Bearer ${token}` }
    })
      .then(res => res.json())
      .then(data => {
        setBankInfo({
          bankName: data.bankName || '',
          bankAccountNumber: data.bankAccountNumber || '',
          bankAccountHolder: data.bankAccountHolder || '',
          bankBranch: data.bankBranch || ''
        });
      });
  }, [token]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await fetch('/api/auth/bank-info', {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(bankInfo)
      });

      const data = await response.json();
      
      if (response.ok) {
        alert('Cập nhật thành công!');
        onUpdate(data.bankInfo);
      } else {
        alert(data.message || 'Cập nhật thất bại');
      }
    } catch (err) {
      alert('Có lỗi xảy ra. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>Tên ngân hàng:</label>
        <input
          type="text"
          value={bankInfo.bankName}
          onChange={(e) => setBankInfo({...bankInfo, bankName: e.target.value})}
          maxLength={100}
        />
      </div>
      
      <div>
        <label>Số tài khoản:</label>
        <input
          type="text"
          value={bankInfo.bankAccountNumber}
          onChange={(e) => setBankInfo({...bankInfo, bankAccountNumber: e.target.value})}
          maxLength={50}
        />
      </div>
      
      <div>
        <label>Tên chủ tài khoản:</label>
        <input
          type="text"
          value={bankInfo.bankAccountHolder}
          onChange={(e) => setBankInfo({...bankInfo, bankAccountHolder: e.target.value})}
          maxLength={100}
        />
      </div>
      
      <div>
        <label>Chi nhánh:</label>
        <input
          type="text"
          value={bankInfo.bankBranch}
          onChange={(e) => setBankInfo({...bankInfo, bankBranch: e.target.value})}
          maxLength={200}
        />
      </div>
      
      <button type="submit" disabled={loading}>
        {loading ? 'Đang lưu...' : 'Lưu thông tin'}
      </button>
    </form>
  );
};
```

---

## Kết Luận

Tài liệu này mô tả đầy đủ các API endpoints mới cho:
1. ✅ Bật/tắt 2FA
2. ✅ Đổi mật khẩu (với validation đầy đủ)
3. ✅ Quản lý thông tin ngân hàng

Tất cả các endpoints đều yêu cầu authentication và có xử lý lỗi đầy đủ.

