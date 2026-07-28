import axios from 'axios'

/** 백엔드 REST API(WebServer/ApiController.cs)와 통신하는 공용 axios 인스턴스. */
export const apiClient = axios.create({
  baseURL: '/api',
})

/** axios 에러에서 백엔드가 반환한 { error: string } 메시지를 최대한 추출한다. */
export function extractErrorMessage(err: unknown): string {
  if (err && typeof err === 'object' && 'response' in err) {
    const response = (err as { response?: { data?: { error?: string } } }).response
    if (response?.data?.error) {
      return response.data.error
    }
  }
  return err instanceof Error ? err.message : String(err)
}
